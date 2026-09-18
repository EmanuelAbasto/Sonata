using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Domain.Entities;

namespace AudioUploader.Domain.Interfaces
{
    public interface ITranscriptRepository
    {
        Task<Transcript> AddAsync(Transcript transcript, CancellationToken cancellationToken = default);
        Task<Transcript?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task UpdateAsync(Transcript transcript, CancellationToken cancellationToken = default);
    }
}