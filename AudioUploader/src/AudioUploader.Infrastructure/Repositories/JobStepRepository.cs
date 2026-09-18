using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Interfaces;
using AudioUploader.Domain.Services;
using AudioUploader.Infrastructure.Data;

namespace AudioUploader.Infrastructure.Repositories
{
    public class JobStepRepository : IJobStepRepository
    {
        private readonly AppDbContext _context;

        public JobStepRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task MarkStepCompletedAsync(Guid publicId, JobStepFlags step, CancellationToken cancellationToken = default)
        {
            if (step == JobStepFlags.None) return;

            int rows = await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.CompletedSteps, j => (JobStepFlags)((int)j.CompletedSteps | (int)step))
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            if (rows == 0) return;

            await RecomputeStatusAsync(publicId, cancellationToken);
        }

        public async Task SetTranscriptAsync(Guid publicId, int transcriptId, CancellationToken cancellationToken = default)
        {
            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.TranscriptId, transcriptId)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task SetLightFileAsync(Guid publicId, int lightFileId, CancellationToken cancellationToken = default)
        {
            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.LightFileId, lightFileId)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task SetFilteredFileAsync(Guid publicId, int filteredFileId, CancellationToken cancellationToken = default)
        {
            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.FilteredFileId, filteredFileId)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task SetSummaryAsync(Guid publicId, string summary, CancellationToken cancellationToken = default)
        {
            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.Summary, summary)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task SetErrorAsync(Guid publicId, string errorMessage, CancellationToken cancellationToken = default)
        {
            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.ErrorMessage, errorMessage)
                    .SetProperty(j => j.Status, Domain.Enums.JobStatus.Failed)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task<JobStepFlags?> GetCompletedStepsAsync(Guid publicId, CancellationToken cancellationToken = default)
        {
            return await _context.Jobs
                .AsNoTracking()
                .Where(j => j.PublicId == publicId)
                .Select(j => (JobStepFlags?)j.CompletedSteps)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task RecomputeStatusAsync(Guid publicId, CancellationToken cancellationToken)
        {
            JobStepFlags? completedSteps = await _context.Jobs
                .AsNoTracking()
                .Where(j => j.PublicId == publicId)
                .Select(j => (JobStepFlags?)j.CompletedSteps)
                .FirstOrDefaultAsync(cancellationToken);

            if (completedSteps == null) return;

            string? errorMessage = await _context.Jobs
                .AsNoTracking()
                .Where(j => j.PublicId == publicId)
                .Select(j => j.ErrorMessage)
                .FirstOrDefaultAsync(cancellationToken);

            JobStatus status = JobStatusCalculator.Compute(completedSteps.Value, errorMessage);

            await _context.Jobs
                .Where(j => j.PublicId == publicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.Status, status),
                    cancellationToken);
        }
    }
}