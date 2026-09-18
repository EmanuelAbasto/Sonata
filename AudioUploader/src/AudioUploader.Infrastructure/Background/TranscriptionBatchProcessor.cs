using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Interfaces;

namespace AudioUploader.Infrastructure.Background
{
    public class TranscriptionBatchProcessor : ITranscriptionBatchProcessor
    {
        private readonly IJobRepository _jobRepository;
        private readonly ITranscriptionService _transcriptionService;
        private readonly IJobMetricsRepository _jobMetricsRepository;
        private readonly ITempFileStorage _tempFileStorage;
        private readonly ILogger<TranscriptionBatchProcessor> _logger;

        public TranscriptionBatchProcessor(
            IJobRepository jobRepository,
            ITranscriptionService transcriptionService,
            IJobMetricsRepository jobMetricsRepository,
            ITempFileStorage tempFileStorage,
            ILogger<TranscriptionBatchProcessor> logger)
        {
            _jobRepository = jobRepository;
            _transcriptionService = transcriptionService;
            _jobMetricsRepository = jobMetricsRepository;
            _tempFileStorage = tempFileStorage;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid batchId, IReadOnlyList<Guid> jobIds, CancellationToken cancellationToken)
        {
            if (jobIds.Count == 0) return;

            List<TranscriptionJobInput> inputs = new List<TranscriptionJobInput>();
            List<int> jobDbIds = new List<int>();

            foreach (Guid jobId in jobIds)
            {
                string localTempPath = _tempFileStorage.Get(jobId);
                if (string.IsNullOrEmpty(localTempPath) || !File.Exists(localTempPath))
                {
                    _logger.LogWarning("Job {JobId} was marked ready but its temp file is gone. Skipping.", jobId);
                    continue;
                }

                Job? job = await _jobRepository.GetByPublicIdAsync(jobId, cancellationToken);
                if (job == null) continue;

                inputs.Add(new TranscriptionJobInput(jobId, localTempPath));
                jobDbIds.Add(job.Id);
            }

            if (inputs.Count == 0) return;

            _logger.LogInformation("Dispatching transcription batch {BatchId} of {Count} jobs", batchId, inputs.Count);
            await TagMetricsAsync(jobDbIds, batchId, "Transcription", inputs.Count, cancellationToken);

            await _transcriptionService.TranscribeBatchAsync(inputs, cancellationToken);
        }

        private async Task TagMetricsAsync(List<int> jobDbIds, Guid batchId, string batchType, int batchSize, CancellationToken cancellationToken)
        {
            foreach (int id in jobDbIds)
            {
                JobMetrics? metrics = await _jobMetricsRepository.GetByJobIdAsync(id, cancellationToken);
                if (metrics != null)
                {
                    metrics.BatchId = batchId;
                    metrics.BatchType = batchType;
                    metrics.BatchSize = batchSize;
                    await _jobMetricsRepository.UpdateAsync(metrics, cancellationToken);
                }
            }
        }
    }
}