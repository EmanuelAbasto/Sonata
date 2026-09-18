using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IJobNotifier
    {
        Task NotifyTranscriptionCompletedAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task NotifySummaryCompletedAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task NotifyJobCompletedAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task NotifyJobFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default);
    }
}