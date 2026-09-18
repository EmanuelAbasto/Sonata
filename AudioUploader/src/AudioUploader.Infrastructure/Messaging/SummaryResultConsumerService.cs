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
    public class SummaryResultConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<SummaryResultConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public SummaryResultConsumerService(
            IServiceProvider serviceProvider,
            IOptions<RabbitMqOptions> options,
            ILogger<SummaryResultConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

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

            _connection = await factory.CreateConnectionAsync("audio-uploader-summary-consumer", cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: _options.SummaryResultsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            _logger.LogInformation("SummaryResultConsumerService connected to RabbitMQ ({Host}:{Port}), queue '{Queue}'",
                _options.Host, _options.Port, _options.SummaryResultsQueue);

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
                    SummaryCompletedMessage? message = JsonSerializer.Deserialize<SummaryCompletedMessage>(json, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Mensaje de resumen vacío o inválido, se descarta.");
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    await HandleMessageAsync(message, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensaje de resumen. Se descarta sin reintentar.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _options.SummaryResultsQueue,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);
        }

        private async Task HandleMessageAsync(SummaryCompletedMessage message, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            IJobStepRepository jobStepRepository = scope.ServiceProvider.GetRequiredService<IJobStepRepository>();
            IJobMetricsRepository jobMetricsRepository = scope.ServiceProvider.GetRequiredService<IJobMetricsRepository>();
            IJobNotifier jobNotifier = scope.ServiceProvider.GetRequiredService<IJobNotifier>();

            if (!message.Success)
            {
                await jobStepRepository.SetErrorAsync(message.JobId, $"Summary failed: {message.Error}", cancellationToken);
                await jobNotifier.NotifyJobFailedAsync(message.JobId, message.Error ?? "Summary failed", cancellationToken);
                return;
            }

            await jobStepRepository.SetSummaryAsync(message.JobId, message.Summary, cancellationToken);
            await jobStepRepository.MarkStepCompletedAsync(message.JobId, JobStepFlags.Summarized, cancellationToken);

            Job? job = await jobRepository.GetByPublicIdAsync(message.JobId, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} no encontrado al procesar resultado de resumen.", message.JobId);
                return;
            }

            JobMetrics? metrics = await jobMetricsRepository.GetByJobIdAsync(job.Id, cancellationToken);
            if (metrics != null)
            {
                metrics.SummaryEnd = DateTime.UtcNow;
                metrics.SummaryStart = metrics.SummaryEnd.Value.AddMilliseconds(-message.DurationMs);
                metrics.ProcessingEnd = DateTime.UtcNow;
                await jobMetricsRepository.UpdateAsync(metrics, cancellationToken);
            }

            await jobNotifier.NotifySummaryCompletedAsync(message.JobId, cancellationToken);
            await jobNotifier.NotifyJobCompletedAsync(message.JobId, cancellationToken);
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