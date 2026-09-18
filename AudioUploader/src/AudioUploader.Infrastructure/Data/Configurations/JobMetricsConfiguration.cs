using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AudioUploader.Domain.Entities;

namespace AudioUploader.Infrastructure.Data.Configurations
{
    public class JobMetricsConfiguration : IEntityTypeConfiguration<JobMetrics>
    {
        public void Configure(EntityTypeBuilder<JobMetrics> builder)
        {
            builder.ToTable("job_metrics");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.JobId)
                .IsRequired();

            builder.HasOne(jm => jm.Job)
                .WithOne()
                .HasForeignKey<JobMetrics>(jm => jm.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Propiedades requeridas (DateTime no nullable)
            builder.Property(x => x.RequestStart)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.RequestEnd)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.ProcessingStart)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.ProcessingEnd)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.UploadStart).IsRequired(false);
            builder.Property(x => x.UploadEnd).IsRequired(false);
            builder.Property(x => x.CompressionStart).IsRequired(false);
            builder.Property(x => x.CompressionEnd).IsRequired(false);
            builder.Property(x => x.TranscriptionStart).IsRequired(false);
            builder.Property(x => x.TranscriptionEnd).IsRequired(false);
            builder.Property(x => x.SummaryStart).IsRequired(false);
            builder.Property(x => x.SummaryEnd).IsRequired(false);

            builder.Property(x => x.BatchId).IsRequired(false);
            builder.Property(x => x.BatchType)
                .IsRequired(false)
                .HasMaxLength(20);
            builder.Property(x => x.BatchSize).IsRequired(false);
        }
    }
}