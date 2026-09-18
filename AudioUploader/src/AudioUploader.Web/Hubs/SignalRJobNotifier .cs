using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using AudioUploader.Application.Ports;
using AudioUploader.Web.Hubs;

namespace AudioUploader.Web.Hubs
{
    public class SignalRJobNotifier : IJobNotifier
    {
        private readonly IHubContext<AudioProcessingHub> _hubContext;

        public SignalRJobNotifier(IHubContext<AudioProcessingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyTranscriptionCompletedAsync(Guid jobId, CancellationToken cancellationToken = default)
            => SendAsync(jobId, "TranscriptionCompleted", "Transcripción completada", cancellationToken);

        public Task NotifySummaryCompletedAsync(Guid jobId, CancellationToken cancellationToken = default)
            => SendAsync(jobId, "SummaryCompleted", "Resumen generado", cancellationToken);

        public Task NotifyJobCompletedAsync(Guid jobId, CancellationToken cancellationToken = default)
            => SendAsync(jobId, "JobCompleted", "Procesamiento completado", cancellationToken);

        public Task NotifyJobFailedAsync(Guid jobId, string errorMessage, CancellationToken cancellationToken = default)
            => _hubContext.Clients
                .Group(AudioProcessingHub.GroupName(jobId))
                .SendAsync("JobFailed", new { jobId, message = errorMessage }, cancellationToken);

        private Task SendAsync(Guid jobId, string eventName, string message, CancellationToken cancellationToken)
            => _hubContext.Clients
                .Group(AudioProcessingHub.GroupName(jobId))
                .SendAsync(eventName, new { jobId, message }, cancellationToken);
    }
}