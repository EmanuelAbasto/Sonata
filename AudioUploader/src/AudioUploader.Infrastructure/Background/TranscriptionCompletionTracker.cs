using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AudioUploader.Infrastructure.Background
{
    public class TranscriptionCompletionTracker
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _completions = new();

        public Task WaitForTranscriptionAsync(Guid jobId)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (_completions.TryAdd(jobId, tcs))
            {
                return tcs.Task;
            }

            if (_completions.TryGetValue(jobId, out var existing))
            {
                return existing.Task;
            }
            return Task.CompletedTask;
        }

        public void Complete(Guid jobId)
        {
            if (_completions.TryRemove(jobId, out var tcs))
            {
                tcs.TrySetResult(true);
            }
        }

        public void Fail(Guid jobId)
        {
            if (_completions.TryRemove(jobId, out var tcs))
            {
                tcs.TrySetResult(false);
            }
        }
    }
}