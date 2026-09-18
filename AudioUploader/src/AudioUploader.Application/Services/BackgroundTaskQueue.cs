using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AudioUploader.Application.Ports;

namespace AudioUploader.Application.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Guid> _queue;

        public BackgroundTaskQueue()
        {
            BoundedChannelOptions options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _queue = Channel.CreateBounded<Guid>(options);
        }

        public async ValueTask QueueJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            if (jobId == Guid.Empty)
            {
                throw new ArgumentException("JobId cannot be empty", nameof(jobId));
            }

            await _queue.Writer.WriteAsync(jobId, cancellationToken);
        }

        public async ValueTask<Guid> DequeueJobAsync(CancellationToken cancellationToken = default)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }

        public int GetQueueLength()
        {
            return _queue.Reader.Count;
        }
    }
}