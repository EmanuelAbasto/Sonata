using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Services
{
    public static class JobStatusCalculator
    {
        public static JobStatus Compute(JobStepFlags steps, string? errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
                return JobStatus.Failed;

            bool uploaded = (steps & JobStepFlags.Uploaded) == JobStepFlags.Uploaded;
            bool compressed = (steps & JobStepFlags.Compressed) == JobStepFlags.Compressed;
            bool transcribed = (steps & JobStepFlags.Transcribed) == JobStepFlags.Transcribed;
            bool summarized = (steps & JobStepFlags.Summarized) == JobStepFlags.Summarized;

            if (uploaded && compressed && transcribed && summarized) return JobStatus.Completed;
            if (transcribed && !summarized) return JobStatus.Summarizing;
            if (uploaded && compressed && !transcribed) return JobStatus.Transcribing;
            if (uploaded && !compressed) return JobStatus.Compressing;
            if (!uploaded) return JobStatus.Pending;

            return JobStatus.Processing;
        }
    }
}