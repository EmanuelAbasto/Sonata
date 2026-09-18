using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface ITranscriptionBatchProcessor
    {
        Task ProcessAsync(Guid batchId, IReadOnlyList<Guid> jobIds, CancellationToken cancellationToken);
    }

    public interface ISummaryBatchProcessor
    {
        Task ProcessAsync(Guid batchId, IReadOnlyList<int> jobDbIds, CancellationToken cancellationToken);
    }
}