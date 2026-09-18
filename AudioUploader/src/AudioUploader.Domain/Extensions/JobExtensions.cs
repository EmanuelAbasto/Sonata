using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Extensions
{
    public static class JobExtensions
    {
        public static string GetDisplayStatus(this Job? job)
        {
            if (job == null) return "Pending";
            if (!string.IsNullOrEmpty(job.ErrorMessage)) return "Failed";

            bool isUploaded = job.CompletedSteps.HasFlag(JobStepFlags.Uploaded);
            bool isCompressed = job.CompletedSteps.HasFlag(JobStepFlags.Compressed);
            bool isTranscribed = job.CompletedSteps.HasFlag(JobStepFlags.Transcribed);
            bool isSummarized = job.CompletedSteps.HasFlag(JobStepFlags.Summarized);

            if (isUploaded && isCompressed && isTranscribed && isSummarized) return "Completed";

            if (isSummarized) return "Summarizing";
            if (isTranscribed) return "Transcribing";
            if (isCompressed) return "Transcribing";
            if (isUploaded) return "Compressing";

            return "Pending";
        }
    }
}