using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.Configuration;
using AudioUploader.Application.Ports;

namespace AudioUploader.Infrastructure.Background
{
    public class JobProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobProcessorBackgroundService> _logger;
        private readonly SemaphoreSlim _processingSemaphore;

        public JobProcessorBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<JobProcessorBackgroundService> logger,
            AppSettings appSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            int maxConcurrent = appSettings.Processing.MaxConcurrentCompressions;
            _processingSemaphore = new SemaphoreSlim(maxConcurrent);
            _logger.LogInformation("Max concurrent compressions: {MaxConcurrent}", maxConcurrent);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobProcessorBackgroundService started.");

            using (IServiceScope initScope = _serviceProvider.CreateScope())
            {
                IBatchQueueService batchQueue = initScope.ServiceProvider.GetRequiredService<IBatchQueueService>();
                batchQueue.Start();
                _logger.LogInformation("BatchQueueService started from BackgroundService.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSingleJobAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            using (IServiceScope stopScope = _serviceProvider.CreateScope())
            {
                IBatchQueueService batchQueue = stopScope.ServiceProvider.GetRequiredService<IBatchQueueService>();
                batchQueue.Stop();
                _logger.LogInformation("BatchQueueService stopped.");
            }

            _logger.LogInformation("JobProcessorBackgroundService stopped.");
        }

        private async Task ProcessSingleJobAsync(CancellationToken cancellationToken)
        {
            await _processingSemaphore.WaitAsync(cancellationToken);
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                IBackgroundTaskQueue queue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();
                IJobOrchestrator orchestrator = scope.ServiceProvider.GetRequiredService<IJobOrchestrator>();

                Guid jobId = await queue.DequeueJobAsync(cancellationToken);
                _logger.LogInformation("Job {JobId} dequeued for processing", jobId);

                await orchestrator.ProcessJobAsync(jobId, cancellationToken);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
    }
}