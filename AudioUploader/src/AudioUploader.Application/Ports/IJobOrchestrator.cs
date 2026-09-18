using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IJobOrchestrator
    {
        Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}