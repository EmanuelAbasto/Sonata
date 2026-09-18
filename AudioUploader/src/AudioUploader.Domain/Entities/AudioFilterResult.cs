using System;
using System.Collections.Generic;
using System.Text;

namespace AudioUploader.Domain.Entities
{
    public class AudioFilterResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public long FileSize { get; set; }
        public decimal? DurationSeconds { get; set; }
        public string? FilterChain { get; set; }
        public string? Error { get; set; }
    }
}
