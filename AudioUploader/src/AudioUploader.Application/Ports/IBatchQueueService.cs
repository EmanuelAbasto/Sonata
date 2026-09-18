using System;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IBatchQueueService
    {
        Task AddToTranscriptionBatchAsync(Guid jobId);
        Task AddToSummaryBatchAsync(int jobId);
        void Start();
        void Stop();
    }
}