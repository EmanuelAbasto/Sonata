using System.Text.Json.Serialization;

namespace AudioUploader.Application.DTOs
{
    public class SummaryResult
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}