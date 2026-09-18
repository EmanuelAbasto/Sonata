using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueJobAsync(Guid jobId, CancellationToken cancellationToken = default);
        ValueTask<Guid> DequeueJobAsync(CancellationToken cancellationToken = default);
        int GetQueueLength();
    }
}