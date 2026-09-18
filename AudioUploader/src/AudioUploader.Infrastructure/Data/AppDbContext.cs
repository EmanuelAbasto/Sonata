using Microsoft.EntityFrameworkCore;
using AudioUploader.Domain.Entities;

namespace AudioUploader.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<Transcript> Transcripts { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobMetrics> JobMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}