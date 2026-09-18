using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AudioUploader.Domain.Entities;

namespace AudioUploader.Infrastructure.Data.Configurations
{
    public class TranscriptConfiguration : IEntityTypeConfiguration<Transcript>
    {
        public void Configure(EntityTypeBuilder<Transcript> builder)
        {
            builder.ToTable("transcripts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Language)
                .HasMaxLength(10)
                .IsRequired(false);

            builder.Property(x => x.ConfidenceScore)
                .HasPrecision(5, 4)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.Job)
                .WithOne(x => x.Transcript)
                .HasForeignKey<Job>(x => x.TranscriptId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}