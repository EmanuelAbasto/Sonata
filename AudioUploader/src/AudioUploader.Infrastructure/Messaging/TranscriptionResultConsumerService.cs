using AudioUploader.Application.Messaging;
using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;
using AudioUploader.Domain.Interfaces;
using AudioUploader.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Infrastructure.Messaging
{
    public class TranscriptionResultConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<TranscriptionResultConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TranscriptionResultConsumerService(
            IServiceProvider serviceProvider,
            IOptions<RabbitMqOptions> options,
            ILogger<TranscriptionResultConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        // 🟢 RabbitMQ.Client v7: CreateConnection/CreateModel ya no existen; todo es async.
        // DispatchConsumersAsync tampoco existe más: en v7 el dispatch async es el único modo.
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync("audio-uploader-transcription-consumer", cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: _options.TranscriptionResultsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            // Un mensaje a la vez: si algo tarda (DB lenta, SignalR caído), no queremos que se
            // acumulen decenas de mensajes "in flight" sin ack.
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            _logger.LogInformation("TranscriptionResultConsumerService connected to RabbitMQ ({Host}:{Port}), queue '{Queue}'",
                _options.Host, _options.Port, _options.TranscriptionResultsQueue);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    TranscriptionCompletedMessage? message = JsonSerializer.Deserialize<TranscriptionCompletedMessage>(json, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Mensaje de transcripción vacío o inválido, se descarta.");
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    await HandleMessageAsync(message, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensaje de transcripción. Se descarta sin reintentar.");
                    // requeue:false a propósito: un mensaje con datos corruptos o un bug de
                    // deserialización reintentado infinitamente satura la cola. Para producción,
                    // acá conviene una dead-letter queue en vez de descartar sin más.
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _options.TranscriptionResultsQueue,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);
        }

        private async Task HandleMessageAsync(TranscriptionCompletedMessage message, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            IJobStepRepository jobStepRepository = scope.ServiceProvider.GetRequiredService<IJobStepRepository>();
            ITranscriptRepository transcriptRepository = scope.ServiceProvider.GetRequiredService<ITranscriptRepository>();
            IJobMetricsRepository jobMetricsRepository = scope.ServiceProvider.GetRequiredService<IJobMetricsRepository>();
            IBatchQueueService batchQueueService = scope.ServiceProvider.GetRequiredService<IBatchQueueService>();
            IJobNotifier jobNotifier = scope.ServiceProvider.GetRequiredService<IJobNotifier>();

            if (!message.Success)
            {
                await jobStepRepository.SetErrorAsync(message.JobId, $"Transcription failed: {message.Error}", cancellationToken);
                await jobNotifier.NotifyJobFailedAsync(message.JobId, message.Error ?? "Transcription failed", cancellationToken);
                return;
            }

            Transcript transcript = new Transcript(message.Text, message.Language, message.Confidence);
            transcript = await transcriptRepository.AddAsync(transcript, cancellationToken);

            await jobStepRepository.SetTranscriptAsync(message.JobId, transcript.Id, cancellationToken);
            await jobStepRepository.MarkStepCompletedAsync(message.JobId, JobStepFlags.Transcribed, cancellationToken);

            Job? job = await jobRepository.GetByPublicIdAsync(message.JobId, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} no encontrado al procesar resultado de transcripción.", message.JobId);
                return;
            }

            JobMetrics? metrics = await jobMetricsRepository.GetByJobIdAsync(job.Id, cancellationToken);
            if (metrics != null)
            {
                metrics.TranscriptionEnd = DateTime.UtcNow;
                metrics.TranscriptionStart = metrics.TranscriptionEnd.Value.AddMilliseconds(-message.DurationMs);
                await jobMetricsRepository.UpdateAsync(metrics, cancellationToken);
            }

            await jobNotifier.NotifyTranscriptionCompletedAsync(message.JobId, cancellationToken);

            await batchQueueService.AddToSummaryBatchAsync(job.Id);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null)
            {
                await _channel.CloseAsync(cancellationToken);
            }
            if (_connection != null)
            {
                await _connection.CloseAsync(cancellationToken);
            }
            await base.StopAsync(cancellationToken);
        }
    }
}