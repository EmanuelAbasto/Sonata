using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Domain.Entities;

namespace AudioUploader.Domain.Interfaces
{
    public interface IJobMetricsRepository
    {
        Task<JobMetrics> AddAsync(JobMetrics metrics, CancellationToken cancellationToken = default);
        Task<JobMetrics?> GetByJobIdAsync(int jobId, CancellationToken cancellationToken = default);
        Task<IEnumerable<JobMetrics>> GetRecentMetricsAsync(int count = 50, CancellationToken cancellationToken = default);
        Task UpdateAsync(JobMetrics metrics, CancellationToken cancellationToken = default);
        Task<IEnumerable<IGrouping<Guid, JobMetrics>>> GetBatchedMetricsAsync(int count = 50, CancellationToken cancellationToken = default);
    }
}