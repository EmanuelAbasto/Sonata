using System;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Interfaces
{
    public interface IJobStepRepository
    {
        Task MarkStepCompletedAsync(Guid publicId, JobStepFlags step, CancellationToken cancellationToken = default);
        Task SetTranscriptAsync(Guid publicId, int transcriptId, CancellationToken cancellationToken = default);
        Task SetLightFileAsync(Guid publicId, int lightFileId, CancellationToken cancellationToken = default);
        Task SetFilteredFileAsync(Guid publicId, int filteredFileId, CancellationToken cancellationToken = default);

        Task SetSummaryAsync(Guid publicId, string summary, CancellationToken cancellationToken = default);
        Task SetErrorAsync(Guid publicId, string errorMessage, CancellationToken cancellationToken = default);
        Task<JobStepFlags?> GetCompletedStepsAsync(Guid publicId, CancellationToken cancellationToken = default);
    }
}