using Microsoft.Extensions.DependencyInjection;
using AudioUploader.Infrastructure.Options;
using AudioUploader.Application.Configuration;

namespace AudioUploader.Web.Startup
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddApplicationConfiguration(
            this IServiceCollection services,
            AppSettings settings,
            RabbitMqOptions rabbitMqOptions,
            AudioFilterOptions audioFilterOptions)
        {
            services.AddSingleton(settings);

            services.Configure<MinioOptions>(options =>
            {
                options.Endpoint = settings.Minio.Endpoint;
                options.PublicEndpoint = settings.Minio.PublicEndpoint;
                options.AccessKey = settings.Minio.AccessKey;
                options.SecretKey = settings.Minio.SecretKey;
                options.BucketName = settings.Minio.Bucket;
                options.UseSsl = settings.Minio.UseSsl;
            });

            services.Configure<FfmpegOptions>(options =>
            {
                options.FfmpegPath = settings.Ffmpeg.Path;
            });

            services.Configure<WhisperOptions>(options =>
            {
                options.ScriptPath = settings.Whisper.ScriptPath;
                options.PythonPath = settings.Whisper.PythonPath;
                options.Model = settings.Whisper.Model;
            });

            services.Configure<SummaryOptions>(options =>
            {
                options.ScriptPath = settings.Summary.ScriptPath;
                options.PythonPath = settings.Summary.PythonPath;
                options.Model = settings.Summary.Model;
                options.OllamaUrl = settings.Summary.OllamaUrl;
                options.MaxLength = settings.Summary.MaxLength;
                options.MinLength = settings.Summary.MinLength;
            });

            services.Configure<ProcessingOptions>(options =>
            {
                options.BatchSize = settings.Processing.BatchSize;
                options.BatchIntervalSeconds = settings.Processing.BatchIntervalSeconds;
            });

            services.Configure<RabbitMqOptions>(options =>
            {
                options.Host = rabbitMqOptions.Host;
                options.Port = rabbitMqOptions.Port;
                options.User = rabbitMqOptions.User;
                options.Password = rabbitMqOptions.Password;
                options.VirtualHost = rabbitMqOptions.VirtualHost;
                options.TranscriptionResultsQueue = rabbitMqOptions.TranscriptionResultsQueue;
                options.SummaryResultsQueue = rabbitMqOptions.SummaryResultsQueue;
            });

            services.Configure<AudioFilterOptions>(options =>
            {
                options.FfmpegPath = audioFilterOptions.FfmpegPath;
                options.FilterChain = audioFilterOptions.FilterChain;
            });

            return services;
        }
    }
}