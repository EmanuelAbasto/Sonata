using System;

namespace AudioUploader.Domain.Enums
{
    [Flags]
    public enum JobStepFlags
    {
        None = 0,
        Uploaded = 1 << 0,
        Compressed = 1 << 1,
        Transcribed = 1 << 2,
        Summarized = 1 << 3,
        Filtered = 1 << 4
    }
}