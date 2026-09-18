using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.DTOs;

namespace AudioUploader.Application.Ports
{
    public interface ISummaryService
    {
        Task GenerateSummariesBatchAsync(IEnumerable<SummaryJobInput> jobs, CancellationToken cancellationToken = default);
    }
}