using System;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Entities
{
    public class AudioFile
    {
        public int Id { get; private set; }
        public string FileName { get; private set; }
        public string? BucketPath { get; set; }
        public long? FileSize { get; private set; }
        public decimal? DurationSeconds { get; private set; }
        public string? Format { get; private set; }
        public FileType FileType { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Job? JobAsOriginal { get; private set; }
        public Job? JobAsLight { get; private set; }

        private AudioFile() { }

        public AudioFile(
            string? bucketPath,
            string fileName,
            FileType fileType,
            long? fileSize = null,
            decimal? durationSeconds = null,
            string? format = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required", nameof(fileName));

            FileName = fileName;
            BucketPath = bucketPath;
            FileType = fileType;
            FileSize = fileSize;
            DurationSeconds = durationSeconds;
            Format = format;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetBucketPath(string bucketPath)
        {
            if (string.IsNullOrWhiteSpace(bucketPath))
                throw new ArgumentException("Bucket path is required", nameof(bucketPath));

            BucketPath = bucketPath;
        }
    }
}