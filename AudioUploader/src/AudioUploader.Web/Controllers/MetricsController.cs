using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Extensions; // <--- ¡IMPORTANTE!
using AudioUploader.Domain.Interfaces;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IJobMetricsRepository _metricsRepository;
    private readonly IJobRepository _jobRepository;

    public MetricsController(IJobMetricsRepository metricsRepository, IJobRepository jobRepository)
    {
        _metricsRepository = metricsRepository;
        _jobRepository = jobRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics([FromQuery] int count = 50)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        IEnumerable<JobMetrics> allMetrics = await _metricsRepository.GetRecentMetricsAsync(count, cancellationToken);
        IEnumerable<IGrouping<Guid, JobMetrics>> batchGroups = await _metricsRepository.GetBatchedMetricsAsync(count, cancellationToken);

        double avgRequestMs = allMetrics.Any() ? allMetrics.Average(m => m.RequestDurationMs) : 0;
        double avgUploadMs = allMetrics.Any() ? allMetrics.Average(m => m.UploadDurationMs) : 0;
        double avgCompressionMs = allMetrics.Any() ? allMetrics.Average(m => m.CompressionDurationMs ?? 0) : 0;
        double avgTranscriptionMs = allMetrics.Any() ? allMetrics.Average(m => m.TranscriptionDurationMs ?? 0) : 0;
        double avgSummaryMs = allMetrics.Any() ? allMetrics.Average(m => m.SummaryDurationMs ?? 0) : 0;
        double avgProcessingTotalMs = allMetrics.Any() ? allMetrics.Average(m => m.ProcessingTotalMs ?? 0) : 0;

        int totalJobs = allMetrics.Count();
        int completedJobs = allMetrics.Count(m => m.Job != null && m.Job.GetDisplayStatus() == "Completed");
        int failedJobs = allMetrics.Count(m => m.Job != null && m.Job.GetDisplayStatus() == "Failed");
        double successRate = totalJobs > 0 ? (double)completedJobs / totalJobs * 100 : 0;

        var timelineData = allMetrics
            .Where(m => m.Job != null)
            .OrderBy(m => m.RequestStart)
            .Select(m => new
            {
                jobId = m.Job.PublicId,
                requestStart = m.RequestStart,
                uploadMs = m.UploadDurationMs,
                compressionMs = m.CompressionDurationMs ?? 0,
                transcriptionMs = m.TranscriptionDurationMs ?? 0,
                summaryMs = m.SummaryDurationMs ?? 0,
                totalMs = m.ProcessingTotalMs ?? 0,
                status = m.Job.GetDisplayStatus()
            });

        var uploadTimes = allMetrics.Where(m => m.UploadStart.HasValue && m.UploadEnd.HasValue).Select(m => m.UploadDurationMs).ToList();
        var compressionTimes = allMetrics.Where(m => m.CompressionStart.HasValue && m.CompressionEnd.HasValue).Select(m => m.CompressionDurationMs ?? 0).ToList();
        var transcriptionTimes = allMetrics.Where(m => m.TranscriptionStart.HasValue && m.TranscriptionEnd.HasValue).Select(m => m.TranscriptionDurationMs ?? 0).ToList();
        var summaryTimes = allMetrics.Where(m => m.SummaryStart.HasValue && m.SummaryEnd.HasValue).Select(m => m.SummaryDurationMs ?? 0).ToList();

        var jobDetails = allMetrics
            .Where(m => m.Job != null)
            .Select(m => new
            {
                jobId = m.Job.PublicId,
                status = m.Job.GetDisplayStatus(),
                completedSteps = m.Job.CompletedSteps.ToString(),
                requestMs = m.RequestDurationMs,
                uploadMs = m.UploadDurationMs,
                compressionMs = m.CompressionDurationMs ?? 0,
                transcriptionMs = m.TranscriptionDurationMs ?? 0,
                summaryMs = m.SummaryDurationMs ?? 0,
                totalMs = m.ProcessingTotalMs ?? 0,
                batchId = m.BatchId,
                batchSize = m.BatchSize,
                batchType = m.BatchType
            });

        List<object> batchInfoList = new List<object>();
        foreach (IGrouping<Guid, JobMetrics> group in batchGroups)
        {
            Guid batchId = group.Key;
            List<JobMetrics> batchMetrics = group.ToList();
            int batchSize = batchMetrics.First().BatchSize ?? batchMetrics.Count;
            string batchType = batchMetrics.First().BatchType ?? "Unknown";
            DateTime? batchStart = batchMetrics.Min(m => m.RequestStart);
            DateTime? batchEnd = batchMetrics.Max(m => m.ProcessingEnd);
            double? avgTotalMs = batchMetrics.Average(m => m.ProcessingTotalMs ?? 0);
            double? avgTranscriptionMsBatch = batchMetrics.Average(m => m.TranscriptionDurationMs ?? 0);
            double? avgSummaryMsBatch = batchMetrics.Average(m => m.SummaryDurationMs ?? 0);
            int successCount = batchMetrics.Count(m => m.Job != null && m.Job.GetDisplayStatus() == "Completed");
            int failCount = batchMetrics.Count(m => m.Job != null && m.Job.GetDisplayStatus() == "Failed");

            batchInfoList.Add(new
            {
                batchId = batchId,
                batchType = batchType,
                batchSize = batchSize,
                jobCount = batchMetrics.Count,
                start = batchStart,
                end = batchEnd,
                averageTotalMs = avgTotalMs,
                averageTranscriptionMs = avgTranscriptionMsBatch,
                averageSummaryMs = avgSummaryMsBatch,
                successCount = successCount,
                failCount = failCount
            });
        }

        object response = new
        {
            samples = allMetrics.Count(),
            totalJobs = totalJobs,
            completedJobs = completedJobs,
            failedJobs = failedJobs,
            successRate = successRate,
            averageRequestMs = avgRequestMs,
            averageUploadMs = avgUploadMs,
            averageCompressionMs = avgCompressionMs,
            averageTranscriptionMs = avgTranscriptionMs,
            averageSummaryMs = avgSummaryMs,
            averageProcessingTotalMs = avgProcessingTotalMs,
            timeline = timelineData,
            uploadTimes = uploadTimes,
            compressionTimes = compressionTimes,
            transcriptionTimes = transcriptionTimes,
            summaryTimes = summaryTimes,
            data = jobDetails,
            batches = batchInfoList
        };

        return Ok(response);
    }
}