using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Infrastructure.Data.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("jobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PublicId)
                .IsRequired()
                .HasDefaultValueSql("gen_random_uuid()");

            builder.HasIndex(x => x.PublicId)
                .IsUnique();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(JobStatus.Pending);

            builder.Property(x => x.Summary)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(x => x.ErrorMessage)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.OriginalFile)
                .WithOne(x => x.JobAsOriginal)
                .HasForeignKey<Job>(x => x.OriginalFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LightFile)
                .WithOne(x => x.JobAsLight)
                .HasForeignKey<Job>(x => x.LightFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Transcript)
                .WithOne(x => x.Job)
                .HasForeignKey<Job>(x => x.TranscriptId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}