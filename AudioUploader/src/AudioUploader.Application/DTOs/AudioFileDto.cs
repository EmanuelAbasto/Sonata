using System;
using System.Text.Json.Serialization;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Application.DTOs
{
    public class AudioFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string BucketPath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public decimal? DurationSeconds { get; set; }
        public string? Format { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FileType FileType { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? JobId { get; set; }
        public string JobStatusDisplay { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }
    }
}