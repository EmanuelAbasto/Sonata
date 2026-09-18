using System;

namespace AudioUploader.Domain.Entities
{
    public class JobMetrics
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        public DateTime RequestStart { get; set; }
        public DateTime RequestEnd { get; set; }
        public DateTime ProcessingStart { get; set; }
        public DateTime ProcessingEnd { get; set; }
        public DateTime? UploadStart { get; set; }
        public DateTime? UploadEnd { get; set; }
        public DateTime? CompressionStart { get; set; }
        public DateTime? CompressionEnd { get; set; }
        public DateTime? TranscriptionStart { get; set; }
        public DateTime? TranscriptionEnd { get; set; }
        public DateTime? SummaryStart { get; set; }
        public DateTime? SummaryEnd { get; set; }

        public Guid? BatchId { get; set; }
        public string? BatchType { get; set; }
        public int? BatchSize { get; set; }
        public long RequestDurationMs => (long)(RequestEnd - RequestStart).TotalMilliseconds;
        public long UploadDurationMs => UploadStart.HasValue && UploadEnd.HasValue
            ? (long)(UploadEnd.Value - UploadStart.Value).TotalMilliseconds
            : 0;
        public long? CompressionDurationMs =>
            CompressionStart.HasValue && CompressionEnd.HasValue
                ? (long?)(CompressionEnd.Value - CompressionStart.Value).TotalMilliseconds
                : null;
        public long? TranscriptionDurationMs =>
            TranscriptionStart.HasValue && TranscriptionEnd.HasValue
                ? (long?)(TranscriptionEnd.Value - TranscriptionStart.Value).TotalMilliseconds
                : null;
        public long? SummaryDurationMs =>
            SummaryStart.HasValue && SummaryEnd.HasValue
                ? (long?)(SummaryEnd.Value - SummaryStart.Value).TotalMilliseconds
                : null;
        public long? ProcessingTotalMs =>
            ProcessingEnd > ProcessingStart
                ? (long?)(ProcessingEnd - ProcessingStart).TotalMilliseconds
                : null;
    }
}