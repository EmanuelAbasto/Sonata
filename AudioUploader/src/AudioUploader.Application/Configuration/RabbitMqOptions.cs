namespace AudioUploader.Infrastructure.Options
{
    public class RabbitMqOptions
    {
        public string Host { get; set; } = "rabbitmq";
        public int Port { get; set; } = 5672;
        public string User { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string TranscriptionResultsQueue { get; set; } = "transcription-results";
        public string SummaryResultsQueue { get; set; } = "summary-results";
    }
}