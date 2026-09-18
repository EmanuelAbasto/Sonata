using System;
using System.Text.Json.Serialization;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Application.DTOs
{
    public class JobStatusResponse
    {
        public Guid JobId { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStepFlags CompletedSteps { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AudioFileDto? OriginalFile { get; set; }
        public AudioFileDto? LightFile { get; set; }
        public AudioFileDto? FilteredFile { get; set; }
    }
}