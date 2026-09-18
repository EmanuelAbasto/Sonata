using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> AddAsync(Job job, CancellationToken cancellationToken = default);
        Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Job?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
        Task<Job?> GetByPublicIdWithFilesAsync(Guid publicId, CancellationToken cancellationToken = default);
        Task UpdateAsync(Job job, CancellationToken cancellationToken = default);
        Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status, int limit, CancellationToken cancellationToken = default);
        Task<IEnumerable<Job>> GetCompletedTranscriptionJobsAsync(int limit, CancellationToken cancellationToken = default);
        Task<IEnumerable<Job>> GetJobsForBatchAsync(int batchSize, JobStatus status, CancellationToken cancellationToken = default);
    }
}