using System;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Entities
{
    public class Job
    {
        public int Id { get; private set; }
        public Guid PublicId { get; private set; }
        public JobStatus Status { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public int OriginalFileId { get; private set; }
        public AudioFile OriginalFile { get; private set; }
        public int? LightFileId { get; private set; }
        public AudioFile? LightFile { get; private set; }

        // 🟢 Nuevo: archivo con el filtro de audio aplicado (ej. reducción de ruido + normalización).
        // Sigue el mismo patrón que LightFile: una referencia opcional a otro AudioFile.
        public int? FilteredFileId { get; private set; }
        public AudioFile? FilteredFile { get; private set; }

        public int? TranscriptId { get; private set; }
        public Transcript? Transcript { get; private set; }
        public string? Summary { get; private set; }

        public JobStepFlags CompletedSteps { get; private set; }

        private Job() { }

        public Job(AudioFile originalFile, Guid? publicId = null)
        {
            if (originalFile == null)
                throw new ArgumentNullException(nameof(originalFile));

            OriginalFile = originalFile;
            OriginalFileId = originalFile.Id;
            PublicId = publicId ?? Guid.NewGuid();
            Status = JobStatus.Pending;
            CompletedSteps = JobStepFlags.None;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkStepCompleted(JobStepFlags step)
        {
            if (step == JobStepFlags.None)
                return;

            CompletedSteps |= step;
            UpdateStatusFromSteps();
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsStepCompleted(JobStepFlags step)
        {
            return (CompletedSteps & step) == step;
        }

        private void UpdateStatusFromSteps()
        {
            // Nota: Filtered NO participa acá a propósito. Es un output adicional en paralelo
            // (como Compressed), pero no es un prerequisito para que el Job se considere
            // Completed — así el filtrado puede fallar o tardar sin bloquear el pipeline
            // principal (transcripción/resumen), que es el que le importa al usuario.
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                Status = JobStatus.Failed;
                return;
            }

            if (IsStepCompleted(JobStepFlags.Uploaded) &&
                IsStepCompleted(JobStepFlags.Compressed) &&
                IsStepCompleted(JobStepFlags.Transcribed) &&
                IsStepCompleted(JobStepFlags.Summarized))
            {
                Status = JobStatus.Completed;
                return;
            }

            if (IsStepCompleted(JobStepFlags.Transcribed) &&
                !IsStepCompleted(JobStepFlags.Summarized))
            {
                Status = JobStatus.Summarizing;
                return;
            }

            if (IsStepCompleted(JobStepFlags.Uploaded) &&
                IsStepCompleted(JobStepFlags.Compressed) &&
                !IsStepCompleted(JobStepFlags.Transcribed))
            {
                Status = JobStatus.Transcribing;
                return;
            }

            if (IsStepCompleted(JobStepFlags.Uploaded) &&
                !IsStepCompleted(JobStepFlags.Compressed))
            {
                Status = JobStatus.Compressing;
                return;
            }

            if (!IsStepCompleted(JobStepFlags.Uploaded))
            {
                Status = JobStatus.Pending;
                return;
            }

            Status = JobStatus.Processing;
        }

        public void SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
            Status = JobStatus.Failed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetLightFile(AudioFile lightFile)
        {
            if (lightFile == null)
                throw new ArgumentNullException(nameof(lightFile));

            LightFile = lightFile;
            LightFileId = lightFile.Id;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetFilteredFile(AudioFile filteredFile)
        {
            if (filteredFile == null)
                throw new ArgumentNullException(nameof(filteredFile));

            FilteredFile = filteredFile;
            FilteredFileId = filteredFile.Id;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetTranscript(Transcript transcript)
        {
            if (transcript == null)
                throw new ArgumentNullException(nameof(transcript));

            Transcript = transcript;
            TranscriptId = transcript.Id;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                throw new ArgumentException("Summary cannot be empty", nameof(summary));

            Summary = summary;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}