namespace AudioUploader.Domain.Enums
{
    public enum JobStatus
    {
        Pending,
        Processing,
        Compressing,
        Transcribing,
        Summarizing,
        Completed,
        Failed
    }
}