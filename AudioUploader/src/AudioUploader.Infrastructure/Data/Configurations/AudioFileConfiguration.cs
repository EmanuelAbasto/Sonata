using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Infrastructure.Data.Configurations
{
    public class AudioFileConfiguration : IEntityTypeConfiguration<AudioFile>
    {
        public void Configure(EntityTypeBuilder<AudioFile> builder)
        {
            builder.ToTable("audio_files");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BucketPath)
                .IsRequired(false)
                .HasMaxLength(512);

            builder.Property(x => x.FileSize)
                .IsRequired(false);

            builder.Property(x => x.DurationSeconds)
                .HasPrecision(10, 2)
                .IsRequired(false);

            builder.Property(x => x.Format)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(x => x.FileType)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.JobAsOriginal)
                .WithOne(x => x.OriginalFile)
                .HasForeignKey<Job>(x => x.OriginalFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.JobAsLight)
                .WithOne(x => x.LightFile)
                .HasForeignKey<Job>(x => x.LightFileId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}