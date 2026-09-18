using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Interfaces;
using AudioUploader.Infrastructure.Data;

namespace AudioUploader.Infrastructure.Repositories
{
    public class TranscriptRepository : ITranscriptRepository
    {
        private readonly AppDbContext _context;

        public TranscriptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Transcript> AddAsync(Transcript transcript, CancellationToken cancellationToken = default)
        {
            await _context.Transcripts.AddAsync(transcript, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return transcript;
        }

        public async Task<Transcript?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Transcripts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(Transcript transcript, CancellationToken cancellationToken = default)
        {
            _context.Transcripts.Update(transcript);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}