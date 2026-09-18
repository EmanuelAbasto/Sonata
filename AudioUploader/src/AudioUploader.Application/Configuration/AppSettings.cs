using System.Collections.Generic;

namespace AudioUploader.Application.Configuration
{
    public class AppSettings
    {
        public DatabaseSettings Database { get; set; } = new();
        public MinioSettings Minio { get; set; } = new();
        public FfmpegSettings Ffmpeg { get; set; } = new();
        public WhisperSettings Whisper { get; set; } = new();
        public SummarySettings Summary { get; set; } = new();
        public ProcessingSettings Processing { get; set; } = new();
        public string HostUrl { get; set; } = string.Empty;
    }

    public class DatabaseSettings
    {
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class MinioSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string PublicEndpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public bool UseSsl { get; set; }
    }

    public class FfmpegSettings
    {
        public string Path { get; set; } = string.Empty;
    }

    public class WhisperSettings
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string PythonPath { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }

    public class SummarySettings
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string PythonPath { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string OllamaUrl { get; set; } = string.Empty;
        public int MaxLength { get; set; }
        public int MinLength { get; set; }
    }

    public class ProcessingSettings
    {
        public int BatchSize { get; set; }
        public int BatchIntervalSeconds { get; set; }
        public int MaxConcurrentCompressions { get; set; }
    }
}