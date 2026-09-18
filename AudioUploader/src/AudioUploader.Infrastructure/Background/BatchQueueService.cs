using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.Configuration;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Interfaces;

namespace AudioUploader.Infrastructure.Background
{
    /// <summary>
    /// Orquestador delgado: solo maneja timers y CUÁNDO correr un batch. Decide qué jobs están
    /// "listos" (clasificación) y delega el trabajo real (hablar con Whisper/Ollama, persistir)
    /// a ITranscriptionBatchProcessor / ISummaryBatchProcessor. La selección segura de batch
    /// (sin bucle infinito) vive en PendingBatchQueue.
    /// </summary>
    public class BatchQueueService : IBatchQueueService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BatchQueueService> _logger;
        private readonly ITempFileStorage _tempFileStorage;

        private readonly int _batchIntervalSeconds;
        private readonly PendingBatchQueue<Guid> _transcriptionQueue;
        private readonly PendingBatchQueue<int> _summaryQueue;

        // 🟢 Gate GLOBAL (no uno por tipo): transcripción y resumen compiten por la misma GPU
        // (Whisper y Ollama corren en el mismo hardware), así que no alcanza con serializar
        // cada cola por separado — hay que asegurar que nunca haya más de UN batch corriendo
        // en todo el proceso, sea de transcripción o de resumen.
        private readonly SemaphoreSlim _processingGate = new SemaphoreSlim(1, 1);

        private Timer? _transcriptionTimer;
        private Timer? _summaryTimer;
        private bool _disposed;

        public BatchQueueService(
            IServiceScopeFactory scopeFactory,
            ILogger<BatchQueueService> logger,
            AppSettings appSettings,
            ITempFileStorage tempFileStorage)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _tempFileStorage = tempFileStorage;

            int batchSize = appSettings.Processing.BatchSize;
            _batchIntervalSeconds = appSettings.Processing.BatchIntervalSeconds;
            _transcriptionQueue = new PendingBatchQueue<Guid>(batchSize);
            _summaryQueue = new PendingBatchQueue<int>(batchSize);

            _logger.LogInformation("BatchQueueService configured. Interval: {Interval}s, Batch size: {BatchSize}",
                _batchIntervalSeconds, batchSize);
        }

        public void Start()
        {
            _transcriptionTimer = new Timer(
                _ => _ = RunTranscriptionCycleAsync(),
                null,
                TimeSpan.FromSeconds(_batchIntervalSeconds),
                TimeSpan.FromSeconds(_batchIntervalSeconds));

            _summaryTimer = new Timer(
                _ => _ = RunSummaryCycleAsync(),
                null,
                TimeSpan.FromSeconds(_batchIntervalSeconds),
                TimeSpan.FromSeconds(_batchIntervalSeconds));

            _logger.LogInformation("BatchQueueService started.");
        }

        public void Stop()
        {
            _transcriptionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _summaryTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("BatchQueueService stopped.");
        }

        public async Task AddToTranscriptionBatchAsync(Guid jobId)
        {
            bool isFull = await _transcriptionQueue.AddAsync(jobId);
            _logger.LogInformation("Job {JobId} added to transcription queue", jobId);
            if (isFull)
            {
                _ = RunTranscriptionCycleAsync();
            }
        }

        public async Task AddToSummaryBatchAsync(int jobDbId)
        {
            if (jobDbId <= 0) return;

            bool isFull = await _summaryQueue.AddAsync(jobDbId);
            _logger.LogInformation("Job {JobDbId} added to summary queue", jobDbId);
            if (isFull)
            {
                _ = RunSummaryCycleAsync();
            }
        }

        private async Task RunTranscriptionCycleAsync()
        {
            // No bloqueante: si ya hay un batch corriendo (de transcripción o de resumen),
            // este trigger se descarta. El próximo tick del timer (o el próximo AddToTranscriptionBatchAsync
            // que llene el batch) va a volver a intentarlo, así que nada queda "perdido" para siempre.
            if (!await _processingGate.WaitAsync(0))
            {
                _logger.LogInformation("Ya hay un batch en proceso; se pospone el ciclo de transcripción.");
                return;
            }

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

                List<Guid> readyJobs = await _transcriptionQueue.TakeReadyBatchAsync(
                    jobId => ClassifyForTranscriptionAsync(jobId, jobRepository));

                if (readyJobs.Count == 0)
                {
                    return;
                }

                ITranscriptionBatchProcessor processor = scope.ServiceProvider.GetRequiredService<ITranscriptionBatchProcessor>();
                await processor.ProcessAsync(Guid.NewGuid(), readyJobs, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transcription batch");
            }
            finally
            {
                _processingGate.Release();
            }
        }

        private async Task RunSummaryCycleAsync()
        {
            if (!await _processingGate.WaitAsync(0))
            {
                _logger.LogInformation("Ya hay un batch en proceso; se pospone el ciclo de resumen.");
                return;
            }

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

                List<int> readyJobs = await _summaryQueue.TakeReadyBatchAsync(
                    jobDbId => ClassifyForSummaryAsync(jobDbId, jobRepository));

                if (readyJobs.Count == 0)
                {
                    return;
                }

                ISummaryBatchProcessor processor = scope.ServiceProvider.GetRequiredService<ISummaryBatchProcessor>();
                await processor.ProcessAsync(Guid.NewGuid(), readyJobs, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing summary batch");
            }
            finally
            {
                _processingGate.Release();
            }
        }

        private async Task<ItemReadiness> ClassifyForTranscriptionAsync(Guid jobId, IJobRepository jobRepository)
        {
            Job? job = await jobRepository.GetByPublicIdAsync(jobId, CancellationToken.None);
            if (job == null) return ItemReadiness.Discard;
            if (job.IsStepCompleted(JobStepFlags.Transcribed)) return ItemReadiness.Discard;
            if (!string.IsNullOrEmpty(job.ErrorMessage)) return ItemReadiness.Discard;

            string localTempPath = _tempFileStorage.Get(jobId);
            bool ready = !string.IsNullOrEmpty(localTempPath) && File.Exists(localTempPath);
            return ready ? ItemReadiness.Ready : ItemReadiness.Deferred;
        }

        private async Task<ItemReadiness> ClassifyForSummaryAsync(int jobDbId, IJobRepository jobRepository)
        {
            Job? job = await jobRepository.GetByIdAsync(jobDbId, CancellationToken.None);
            if (job == null) return ItemReadiness.Discard;
            if (job.IsStepCompleted(JobStepFlags.Summarized)) return ItemReadiness.Discard;
            if (!string.IsNullOrEmpty(job.ErrorMessage)) return ItemReadiness.Discard;

            bool ready = job.IsStepCompleted(JobStepFlags.Transcribed) && job.Transcript != null;
            return ready ? ItemReadiness.Ready : ItemReadiness.Deferred;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _transcriptionTimer?.Dispose();
            _summaryTimer?.Dispose();
            _processingGate.Dispose();
            _disposed = true;
        }
    }
}