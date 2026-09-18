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
    public class JobMetricsRepository : IJobMetricsRepository
    {
        private readonly AppDbContext _context;

        public JobMetricsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<JobMetrics> AddAsync(JobMetrics metrics, CancellationToken cancellationToken = default)
        {
            await _context.JobMetrics.AddAsync(metrics, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return metrics;
        }

        public async Task<JobMetrics?> GetByJobIdAsync(int jobId, CancellationToken cancellationToken = default)
        {
            return await _context.JobMetrics
                .Include(m => m.Job)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.JobId == jobId, cancellationToken);
        }

        public async Task<IEnumerable<JobMetrics>> GetRecentMetricsAsync(int count = 50, CancellationToken cancellationToken = default)
        {
            return await _context.JobMetrics
                .Include(m => m.Job)
                .AsNoTracking()
                .OrderByDescending(m => m.RequestStart)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(JobMetrics metrics, CancellationToken cancellationToken = default)
        {
            var trackedEntity = _context.ChangeTracker.Entries<JobMetrics>()
                .FirstOrDefault(e => e.Entity.Id == metrics.Id);

            if (trackedEntity != null)
            {
                _context.Entry(trackedEntity.Entity).CurrentValues.SetValues(metrics);
            }
            else
            {
                _context.Entry(metrics).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<IGrouping<Guid, JobMetrics>>> GetBatchedMetricsAsync(int count = 50, CancellationToken cancellationToken = default)
        {
            List<JobMetrics> metrics = await _context.JobMetrics
                .Include(m => m.Job)
                .AsNoTracking()
                .Where(m => m.BatchId.HasValue)
                .OrderByDescending(m => m.RequestStart)
                .Take(count)
                .ToListAsync(cancellationToken);

            return metrics
                .GroupBy(m => m.BatchId.Value);
        }
    }
}