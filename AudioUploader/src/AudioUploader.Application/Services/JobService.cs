using System;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Extensions;
using AudioUploader.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioUploader.Application.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        private readonly IStorageService _storageService;
        private readonly ILogger<JobService> _logger;

        private const int PresignedUrlExpirySeconds = 3600;

        public JobService(IJobRepository jobRepository, IStorageService storageService, ILogger<JobService> logger)
        {
            _jobRepository = jobRepository;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<JobStatusResponse?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            Job? job = await _jobRepository.GetByPublicIdWithFilesAsync(jobId, cancellationToken);
            if (job == null)
            {
                return null;
            }

            JobStatusResponse response = new JobStatusResponse
            {
                JobId = job.PublicId,
                StatusDisplay = job.GetDisplayStatus(),
                Status = job.Status,
                CompletedSteps = job.CompletedSteps,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt,
                OriginalFile = await MapAudioFileToDtoAsync(job.OriginalFile)
            };

            if (job.LightFile != null)
            {
                response.LightFile = await MapAudioFileToDtoAsync(job.LightFile);
            }

            if (job.FilteredFile != null)
            {
                response.FilteredFile = await MapAudioFileToDtoAsync(job.FilteredFile);
            }

            return response;
        }

        public async Task<string?> GetTranscriptionTextAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            Job? job = await _jobRepository.GetByPublicIdAsync(jobId, cancellationToken);
            if (job == null || !job.TranscriptId.HasValue)
            {
                return null;
            }

            if (job.Transcript == null)
            {
                return null;
            }

            return job.Transcript.Text;
        }

        public async Task<string?> GetSummaryAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            Job? job = await _jobRepository.GetByPublicIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                return null;
            }

            return job.Summary;
        }

        private async Task<AudioFileDto> MapAudioFileToDtoAsync(AudioFile file)
        {
            string? audioUrl = await BuildAudioUrlAsync(file);

            return new AudioFileDto
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
        }

        private async Task<string?> BuildAudioUrlAsync(AudioFile file)
        {
            if (string.IsNullOrEmpty(file.BucketPath))
            {
                return null;
            }

            string bucketName = _storageService.GetBucketName();
            string presignedUrl = await _storageService.GetPresignedUrlAsync(bucketName, file.BucketPath, PresignedUrlExpirySeconds);
            return presignedUrl;
        }
    }

    public interface IJobService
    {
        Task<JobStatusResponse?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task<string?> GetTranscriptionTextAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task<string?> GetSummaryAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}