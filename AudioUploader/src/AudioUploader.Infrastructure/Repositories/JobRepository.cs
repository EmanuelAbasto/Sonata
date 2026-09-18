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
    public class JobRepository : IJobRepository
    {
        private readonly AppDbContext _context;

        public JobRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Job> AddAsync(Job job, CancellationToken cancellationToken = default)
        {
            await _context.Jobs.AddAsync(job, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return job;
        }

        public async Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Include(x => x.OriginalFile)
                .Include(x => x.LightFile)
                .Include(x => x.FilteredFile)
                .Include(x => x.Transcript)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<Job?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Include(x => x.OriginalFile)
                .Include(x => x.LightFile)
                .Include(x => x.FilteredFile)
                .Include(x => x.Transcript)
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);
        }

        public async Task<Job?> GetByPublicIdWithFilesAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Include(x => x.OriginalFile)
                .Include(x => x.LightFile)
                .Include(x => x.FilteredFile)
                .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);
        }

        public async Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
        {
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status, int limit, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Where(x => x.Status == status)
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .Include(x => x.OriginalFile)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Job>> GetCompletedTranscriptionJobsAsync(int limit, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Where(x => x.Status == JobStatus.Transcribing && x.TranscriptId != null)
                .OrderBy(x => x.CreatedAt)
                .Take(limit)
                .Include(x => x.Transcript)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Job>> GetJobsForBatchAsync(int batchSize, JobStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .Where(x => x.Status == status)
                .OrderBy(x => x.CreatedAt)
                .Take(batchSize)
                .Include(x => x.OriginalFile)
                .Include(x => x.Transcript)
                .ToListAsync(cancellationToken);
        }
    }
}