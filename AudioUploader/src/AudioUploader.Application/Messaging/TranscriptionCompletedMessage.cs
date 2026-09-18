using System;

namespace AudioUploader.Application.Messaging
{
    public class TranscriptionCompletedMessage
    {
        public Guid JobId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; }
        public decimal? Confidence { get; set; }
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}