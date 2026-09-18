using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.DTOs;

namespace AudioUploader.Application.Ports
{
    public interface ITranscriptionService
    {
        Task TranscribeBatchAsync(IEnumerable<TranscriptionJobInput> jobs, CancellationToken cancellationToken = default);
    }
}