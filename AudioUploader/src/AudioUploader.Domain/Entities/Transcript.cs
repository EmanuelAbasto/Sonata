using System;

namespace AudioUploader.Domain.Entities
{
    public class Transcript
    {
        public int Id { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public string? Language { get; private set; }
        public decimal? ConfidenceScore { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Job? Job { get; private set; }

        private Transcript() { }

        public Transcript(string text, string? language = null, decimal? confidenceScore = null)
        {
            Text = text;
            Language = language;
            ConfidenceScore = confidenceScore;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateText(string text, string? language, decimal? confidenceScore)
        {
            Text = text;
            Language = language;
            ConfidenceScore = confidenceScore;
        }
    }
}