using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Interfaces;
using AudioUploader.Infrastructure.Data;

namespace AudioUploader.Infrastructure.Repositories
{
    public class AudioFileRepository : IAudioFileRepository
    {
        private readonly AppDbContext _context;

        public AudioFileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AudioFile> AddAsync(AudioFile audioFile, CancellationToken cancellationToken = default)
        {
            await _context.AudioFiles.AddAsync(audioFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return audioFile;
        }

        public async Task<AudioFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.AudioFiles
                .Include(x => x.JobAsOriginal)
                    .ThenInclude(x => x!.LightFile)
                .Include(x => x.JobAsOriginal)
                    .ThenInclude(x => x!.FilteredFile)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<AudioFile?> GetByBucketPathAsync(string bucketPath, CancellationToken cancellationToken = default)
        {
            return await _context.AudioFiles
                .FirstOrDefaultAsync(x => x.BucketPath == bucketPath, cancellationToken);
        }

        public async Task UpdateAsync(AudioFile audioFile, CancellationToken cancellationToken = default)
        {
            _context.AudioFiles.Update(audioFile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<(IEnumerable<AudioFile> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? format = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<AudioFile> query = _context.AudioFiles
                .Include(x => x.JobAsOriginal)
                .AsNoTracking()
                .Where(x => x.FileType == FileType.Original)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.FileName.Contains(searchTerm) || x.BucketPath.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobStatus>(status, true, out JobStatus statusEnum))
            {
                query = query.Where(x => x.JobAsOriginal != null && x.JobAsOriginal.Status == statusEnum);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(format))
            {
                query = query.Where(x => x.Format != null && x.Format.ToLower() == format.ToLower());
            }

            int totalCount = await query.CountAsync(cancellationToken);

            IEnumerable<AudioFile> items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}