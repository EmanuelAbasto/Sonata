// AudioUploader.Infrastructure.Background.JobOrchestrator.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioUploader.Infrastructure.Background
{
    public class JobOrchestrator : IJobOrchestrator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IStorageService _storageService;
        private readonly IAudioCompressor _audioCompressor;
        private readonly IAudioFilterService _audioFilterService;
        private readonly IBatchQueueService _batchQueueService;
        private readonly ITempFileStorage _tempFileStorage;
        private readonly ILogger<JobOrchestrator> _logger;

        public JobOrchestrator(
            IServiceScopeFactory serviceScopeFactory,
            IStorageService storageService,
            IAudioCompressor audioCompressor,
            IAudioFilterService audioFilterService,
            IBatchQueueService batchQueueService,
            ITempFileStorage tempFileStorage,
            ILogger<JobOrchestrator> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _storageService = storageService;
            _audioCompressor = audioCompressor;
            _audioFilterService = audioFilterService;
            _batchQueueService = batchQueueService;
            _tempFileStorage = tempFileStorage;
            _logger = logger;
        }

        public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            string tempPath = _tempFileStorage.Get(jobId);
            if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
            {
                _logger.LogError("Temporary file not found for job {JobId}", jobId);
                return;
            }

            string bucketName = _storageService.GetBucketName();

            // 1. Encolar transcripción de inmediato (no espera nada; corre por batch).
            _ = _batchQueueService.AddToTranscriptionBatchAsync(jobId);

            // 2. Upload a MinIO, en paralelo.
            _ = Task.Run(async () =>
            {
                using IServiceScope uploadScope = _serviceScopeFactory.CreateScope();
                IJobRepository jobRepo = uploadScope.ServiceProvider.GetRequiredService<IJobRepository>();
                IJobStepRepository jobStepRepo = uploadScope.ServiceProvider.GetRequiredService<IJobStepRepository>();
                IAudioFileRepository audioFileRepo = uploadScope.ServiceProvider.GetRequiredService<IAudioFileRepository>();
                IJobMetricsRepository metricsRepo = uploadScope.ServiceProvider.GetRequiredService<IJobMetricsRepository>();

                try
                {
                    Job? freshJob = await jobRepo.GetByPublicIdWithFilesAsync(jobId, cancellationToken);
                    if (freshJob == null) return;

                    DateTime uploadStart = DateTime.UtcNow;
                    string generatedObjectName = _storageService.GenerateObjectName(freshJob.OriginalFile.FileName);

                    using (FileStream fs = File.OpenRead(tempPath))
                    {
                        await _storageService.UploadFileAsync(bucketName, generatedObjectName, fs, "audio/mpeg", cancellationToken);
                    }

                    freshJob.OriginalFile.SetBucketPath(generatedObjectName);
                    await audioFileRepo.UpdateAsync(freshJob.OriginalFile, cancellationToken);

                    DateTime uploadEnd = DateTime.UtcNow;
                    JobMetrics? freshMetrics = await metricsRepo.GetByJobIdAsync(freshJob.Id, cancellationToken);
                    if (freshMetrics != null)
                    {
                        freshMetrics.UploadStart = uploadStart;
                        freshMetrics.UploadEnd = uploadEnd;
                        await metricsRepo.UpdateAsync(freshMetrics, cancellationToken);
                    }

                    await jobStepRepo.MarkStepCompletedAsync(jobId, JobStepFlags.Uploaded, cancellationToken);
                    _logger.LogInformation("Upload to MinIO completed for job {JobId}", jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload failed for job {JobId}", jobId);
                    await jobStepRepo.SetErrorAsync(jobId, $"Upload failed: {ex.Message}", cancellationToken);
                }
            }, cancellationToken);

            // 3. Compresión (versión "light"), en paralelo.
            _ = Task.Run(async () =>
            {
                using IServiceScope compressionScope = _serviceScopeFactory.CreateScope();
                IJobRepository jobRepo = compressionScope.ServiceProvider.GetRequiredService<IJobRepository>();
                IJobStepRepository jobStepRepo = compressionScope.ServiceProvider.GetRequiredService<IJobStepRepository>();
                IAudioFileRepository audioFileRepo = compressionScope.ServiceProvider.GetRequiredService<IAudioFileRepository>();
                IJobMetricsRepository metricsRepo = compressionScope.ServiceProvider.GetRequiredService<IJobMetricsRepository>();

                try
                {
                    DateTime compressionStart = DateTime.UtcNow;
                    string lightOutputPath = Path.GetTempFileName() + ".aac";
                    CompressionResult result = await _audioCompressor.CompressAsync(tempPath, lightOutputPath, cancellationToken);
                    if (!result.Success)
                    {
                        throw new InvalidOperationException($"Compression failed: {result.Error}");
                    }

                    string lightObjectName = $"compressed/{Guid.NewGuid():N}.aac";
                    using (FileStream fs = File.OpenRead(lightOutputPath))
                    {
                        await _storageService.UploadFileAsync(bucketName, lightObjectName, fs, "audio/aac", cancellationToken);
                    }

                    AudioFile lightFile = new AudioFile(
                        bucketPath: lightObjectName,
                        fileName: $"light_{Path.GetFileNameWithoutExtension(tempPath)}.aac",
                        fileType: FileType.Light,
                        fileSize: result.FileSize,
                        durationSeconds: result.DurationSeconds,
                        format: "aac"
                    );
                    lightFile = await audioFileRepo.AddAsync(lightFile, cancellationToken);

                    if (File.Exists(lightOutputPath)) File.Delete(lightOutputPath);

                    DateTime compressionEnd = DateTime.UtcNow;

                    Job? jobForMetrics = await jobRepo.GetByPublicIdAsync(jobId, cancellationToken);
                    if (jobForMetrics == null) return;

                    JobMetrics? freshMetrics = await metricsRepo.GetByJobIdAsync(jobForMetrics.Id, cancellationToken);
                    if (freshMetrics != null)
                    {
                        freshMetrics.CompressionStart = compressionStart;
                        freshMetrics.CompressionEnd = compressionEnd;
                        await metricsRepo.UpdateAsync(freshMetrics, cancellationToken);
                    }

                    await jobStepRepo.SetLightFileAsync(jobId, lightFile.Id, cancellationToken);
                    await jobStepRepo.MarkStepCompletedAsync(jobId, JobStepFlags.Compressed, cancellationToken);

                    _logger.LogInformation("Compression completed for job {JobId}", jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Compression failed for job {JobId}", jobId);
                    await jobStepRepo.SetErrorAsync(jobId, $"Compression failed: {ex.Message}", cancellationToken);
                }
            }, cancellationToken);

            // 4. 🟢 NUEVO: filtro de audio (reducción de ruido + normalización), en paralelo.
            // Mismo patrón que compresión: no bloquea nada, y su éxito/fracaso no afecta el
            // Status del Job (Filtered no participa del cálculo de status — ver Job.cs).
            _ = Task.Run(async () =>
            {
                using IServiceScope filterScope = _serviceScopeFactory.CreateScope();
                IJobRepository jobRepo = filterScope.ServiceProvider.GetRequiredService<IJobRepository>();
                IJobStepRepository jobStepRepo = filterScope.ServiceProvider.GetRequiredService<IJobStepRepository>();
                IAudioFileRepository audioFileRepo = filterScope.ServiceProvider.GetRequiredService<IAudioFileRepository>();

                string filteredOutputPath = Path.GetTempFileName() + ".aac";
                try
                {
                    AudioFilterResult result = await _audioFilterService.ApplyFilterAsync(tempPath, filteredOutputPath, cancellationToken);
                    if (!result.Success)
                    {
                        _logger.LogWarning("Audio filter failed for job {JobId}: {Error}", jobId, result.Error);
                        return; // no es un error fatal del Job: el pipeline principal sigue igual
                    }

                    string filteredObjectName = $"filtered/{Guid.NewGuid():N}.aac";
                    using (FileStream fs = File.OpenRead(filteredOutputPath))
                    {
                        await _storageService.UploadFileAsync(bucketName, filteredObjectName, fs, "audio/aac", cancellationToken);
                    }

                    AudioFile filteredFile = new AudioFile(
                        bucketPath: filteredObjectName,
                        fileName: $"filtered_{Path.GetFileNameWithoutExtension(tempPath)}.aac",
                        fileType: FileType.Filtered,
                        fileSize: result.FileSize,
                        durationSeconds: result.DurationSeconds,
                        format: "aac"
                    );
                    filteredFile = await audioFileRepo.AddAsync(filteredFile, cancellationToken);

                    await jobStepRepo.SetFilteredFileAsync(jobId, filteredFile.Id, cancellationToken);
                    await jobStepRepo.MarkStepCompletedAsync(jobId, JobStepFlags.Filtered, cancellationToken);

                    _logger.LogInformation("Audio filter ({FilterChain}) completed for job {JobId}", result.FilterChain, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Audio filter failed for job {JobId}", jobId);
                }
                finally
                {
                    if (File.Exists(filteredOutputPath)) File.Delete(filteredOutputPath);
                }
            }, cancellationToken);

            _logger.LogInformation("Job {JobId} tasks launched: transcription queued, upload, compression and filtering started.", jobId);
        }
    }
}