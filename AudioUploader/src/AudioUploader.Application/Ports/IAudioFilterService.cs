using AudioUploader.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{

    public interface IAudioFilterService
    {
        Task<AudioFilterResult> ApplyFilterAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);
    }
}