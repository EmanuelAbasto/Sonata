using Microsoft.Extensions.DependencyInjection;
using AudioUploader.Application.Ports;
using AudioUploader.Application.Services;
using AudioUploader.Domain.Interfaces;
using AudioUploader.Infrastructure.Background;
using AudioUploader.Infrastructure.Messaging;
using AudioUploader.Infrastructure.Services;
using AudioUploader.Infrastructure.Repositories;
using AudioUploader.Infrastructure.Storage;
using AudioUploader.Web.Hubs;

namespace AudioUploader.Web.Startup
{
    public static class DependencyInjectionExtensions
    {

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAudioFileRepository, AudioFileRepository>();
            services.AddScoped<ITranscriptRepository, TranscriptRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobMetricsRepository, JobMetricsRepository>();
            services.AddScoped<IJobStepRepository, JobStepRepository>();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IStorageService, MinioStorageService>();
            services.AddScoped<IAudioCompressor, FfmpegAudioCompressor>();
            services.AddScoped<IAudioFilterService, FfmpegAudioFilterService>();
            services.AddSingleton<ITempFileStorage, TempFileStorage>();
            services.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
            services.AddScoped<ISummaryService, PythonSummaryService>();
            services.AddScoped<IAudioService, AudioService>();
            services.AddScoped<IJobService, JobService>();
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<IBatchQueueService, BatchQueueService>();
            services.AddScoped<IJobOrchestrator, JobOrchestrator>();
            services.AddHostedService<JobProcessorBackgroundService>();
            services.AddScoped<ITranscriptionBatchProcessor, TranscriptionBatchProcessor>();
            services.AddScoped<ISummaryBatchProcessor, SummaryBatchProcessor>();
            return services;
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services)
        {
            services.AddScoped<IJobNotifier, SignalRJobNotifier>();
            services.AddHostedService<TranscriptionResultConsumerService>();
            services.AddHostedService<SummaryResultConsumerService>();
            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Audio Uploader API",
                    Version = "2.2.0",
                    Description = "API para subir archivos de audio y gestionar su procesamiento (transcripción, resumen, compresión, filtrado)."
                });
            });
            return services;
        }
    }
}