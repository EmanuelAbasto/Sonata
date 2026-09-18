using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Interfaces;

namespace AudioUploader.Infrastructure.Background
{
    public class SummaryBatchProcessor : ISummaryBatchProcessor
    {
        private readonly IJobRepository _jobRepository;
        private readonly ISummaryService _summaryService;
        private readonly IJobMetricsRepository _jobMetricsRepository;
        private readonly ILogger<SummaryBatchProcessor> _logger;

        public SummaryBatchProcessor(
            IJobRepository jobRepository,
            ISummaryService summaryService,
            IJobMetricsRepository jobMetricsRepository,
            ILogger<SummaryBatchProcessor> logger)
        {
            _jobRepository = jobRepository;
            _summaryService = summaryService;
            _jobMetricsRepository = jobMetricsRepository;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid batchId, IReadOnlyList<int> jobDbIds, CancellationToken cancellationToken)
        {
            if (jobDbIds.Count == 0) return;

            List<Job> jobs = new List<Job>();
            foreach (int id in jobDbIds)
            {
                Job? job = await _jobRepository.GetByIdAsync(id, cancellationToken);
                if (job?.Transcript == null) continue;
                jobs.Add(job);
            }

            if (jobs.Count == 0) return;

            _logger.LogInformation("Dispatching summary batch {BatchId} of {Count} jobs", batchId, jobs.Count);
            await TagMetricsAsync(jobs.Select(j => j.Id).ToList(), batchId, "Summary", jobs.Count, cancellationToken);

            List<SummaryJobInput> inputs = jobs
                .Select(j => new SummaryJobInput(j.PublicId, j.Transcript!.Text))
                .ToList();

            await _summaryService.GenerateSummariesBatchAsync(inputs, cancellationToken);
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