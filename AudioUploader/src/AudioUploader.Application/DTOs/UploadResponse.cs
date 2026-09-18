using System;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Application.DTOs
{
    public class UploadResponse
    {
        public Guid JobId { get; set; }
        public int AudioId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
    }
}