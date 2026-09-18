// AudioUploader.Application.Services.AudioService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Extensions;
using AudioUploader.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioUploader.Application.Services
{
    public class AudioService : IAudioService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ITempFileStorage _tempFileStorage;
        private readonly ILogger<AudioService> _logger;

        private const int PresignedUrlExpirySeconds = 3600;

        public AudioService(
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue taskQueue,
            ITempFileStorage tempFileStorage,
            ILogger<AudioService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _taskQueue = taskQueue;
            _tempFileStorage = tempFileStorage;
            _logger = logger;
        }

        // --- Métodos de subida (optimizados) ---

        public async Task<IEnumerable<UploadResponse>> UploadFilesAsync(
            IEnumerable<Stream> fileStreams,
            IEnumerable<string> fileNames,
            CancellationToken cancellationToken = default)
        {
            List<Stream> streamList = new List<Stream>(fileStreams);
            List<string> nameList = new List<string>(fileNames);

            if (streamList.Count != nameList.Count)
            {
                throw new ArgumentException("The number of streams and file names must match.");
            }

            const int maxConcurrency = 5;
            using SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);

            List<Task<UploadResponse>> tasks = new List<Task<UploadResponse>>(streamList.Count);

            for (int i = 0; i < streamList.Count; i++)
            {
                Stream stream = streamList[i];
                string fileName = nameList[i];
                Task<UploadResponse> task = ProcessWithSemaphoreAndScopeAsync(stream, fileName, semaphore, cancellationToken);
                tasks.Add(task);
            }

            UploadResponse[] results = await Task.WhenAll(tasks);
            return new List<UploadResponse>(results);
        }

        private async Task<UploadResponse> ProcessWithSemaphoreAndScopeAsync(
            Stream fileStream,
            string fileName,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                IAudioFileRepository audioFileRepository = scope.ServiceProvider.GetRequiredService<IAudioFileRepository>();
                IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                IJobMetricsRepository jobMetricsRepository = scope.ServiceProvider.GetRequiredService<IJobMetricsRepository>();

                return await ProcessSingleFileAsync(
                    fileStream,
                    fileName,
                    audioFileRepository,
                    jobRepository,
                    jobMetricsRepository,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<UploadResponse> ProcessSingleFileAsync(
            Stream fileStream,
            string fileName,
            IAudioFileRepository audioFileRepository,
            IJobRepository jobRepository,
            IJobMetricsRepository jobMetricsRepository,
            CancellationToken cancellationToken)
        {
            Guid jobId = Guid.NewGuid();
            string tempDir = Path.Combine(Path.GetTempPath(), "audio_uploads");
            Directory.CreateDirectory(tempDir);
            string tempFilePath = Path.Combine(tempDir, $"{jobId:N}.mp3");

            using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fs, cancellationToken);
            }

            _logger.LogInformation("Temporary file saved at {TempPath} for job {JobId}", tempFilePath, jobId);

            _tempFileStorage.Add(jobId, tempFilePath);

            AudioFile audioFile = new AudioFile(
                bucketPath: null,
                fileName: fileName,
                fileType: FileType.Original,
                fileSize: new FileInfo(tempFilePath).Length
            );
            audioFile = await audioFileRepository.AddAsync(audioFile, cancellationToken);

            Job job = new Job(audioFile, jobId);
            job = await jobRepository.AddAsync(job, cancellationToken);

            JobMetrics metrics = new JobMetrics
            {
                JobId = job.Id,
                RequestStart = DateTime.UtcNow,
                RequestEnd = DateTime.UtcNow,
                UploadStart = null,
                UploadEnd = null
            };
            await jobMetricsRepository.AddAsync(metrics, cancellationToken);

            await _taskQueue.QueueJobAsync(job.PublicId, cancellationToken);
            _logger.LogInformation("Job {JobId} queued for background processing", job.PublicId);

            // 7. Devolver respuesta inmediata
            return new UploadResponse
            {
                JobId = job.PublicId,
                AudioId = audioFile.Id,
                FileName = audioFile.FileName,
                Status = job.Status
            };
        }

        // --- Métodos de consulta (existentes) ---

        public async Task<PagedResult<AudioFileDto>> GetAudioFilesPagedAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? format = null,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IAudioFileRepository audioFileRepository = scope.ServiceProvider.GetRequiredService<IAudioFileRepository>();
            IStorageService storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            (IEnumerable<AudioFile> items, int totalCount) = await audioFileRepository.GetPagedAsync(
                page, pageSize, searchTerm, status, fromDate, toDate, format, cancellationToken);

            List<AudioFileDto> dtos = new List<AudioFileDto>();
            foreach (AudioFile file in items)
            {
                AudioFileDto dto = await MapToDtoAsync(file, storageService);
                dtos.Add(dto);
            }

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PagedResult<AudioFileDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<AudioDetailResponse?> GetAudioDetailAsync(int audioId, CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IAudioFileRepository audioFileRepository = scope.ServiceProvider.GetRequiredService<IAudioFileRepository>();
            IStorageService storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            AudioFile? audioFile = await audioFileRepository.GetByIdAsync(audioId, cancellationToken);
            if (audioFile == null)
                return null;

            string? audioUrl = await BuildAudioUrlAsync(audioFile, storageService);

            AudioDetailResponse response = new AudioDetailResponse
            {
                Id = audioFile.Id,
                FileName = audioFile.FileName,
                BucketPath = audioFile.BucketPath,
                FileSize = audioFile.FileSize,
                DurationSeconds = audioFile.DurationSeconds,
                Format = audioFile.Format,
                FileType = audioFile.FileType,
                CreatedAt = audioFile.CreatedAt,
                AudioUrl = audioUrl
            };

            if (audioFile.JobAsOriginal != null)
            {
                response.JobId = audioFile.JobAsOriginal.PublicId;
                response.JobStatusDisplay = audioFile.JobAsOriginal.GetDisplayStatus();
            }

            if (audioFile.JobAsOriginal?.LightFile != null)
            {
                AudioFile light = audioFile.JobAsOriginal.LightFile;
                response.LightFile = await MapToDtoAsync(light, storageService);
            }

            if (audioFile.JobAsOriginal?.FilteredFile != null)
            {
                AudioFile filtered = audioFile.JobAsOriginal.FilteredFile;
                response.FilteredFile = await MapToDtoAsync(filtered, storageService);
            }

            return response;
        }

        private async Task<AudioFileDto> MapToDtoAsync(AudioFile file, IStorageService storageService)
        {
            string? audioUrl = await BuildAudioUrlAsync(file, storageService);

            AudioFileDto dto = new AudioFileDto
            {
                Id = file.Id,
                FileName = file.FileName,
                BucketPath = file.BucketPath,
                FileSize = file.FileSize,
                DurationSeconds = file.DurationSeconds,
                Format = file.Format,
                FileType = file.FileType,
                CreatedAt = file.CreatedAt,
                AudioUrl = audioUrl
            };

            if (file.JobAsOriginal != null)
            {
                dto.JobId = file.JobAsOriginal.PublicId;
                dto.JobStatusDisplay = file.JobAsOriginal.GetDisplayStatus();
            }

            return dto;
        }

        private static async Task<string?> BuildAudioUrlAsync(AudioFile file, IStorageService storageService)
        {
            if (string.IsNullOrEmpty(file.BucketPath))
            {
                return null;
            }

            string bucketName = storageService.GetBucketName();
            string presignedUrl = await storageService.GetPresignedUrlAsync(bucketName, file.BucketPath, PresignedUrlExpirySeconds);
            return presignedUrl;
        }
    }
}