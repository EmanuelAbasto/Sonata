using System;

namespace AudioUploader.Application.Messaging
{
    public class SummaryCompletedMessage
    {
        public Guid JobId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}