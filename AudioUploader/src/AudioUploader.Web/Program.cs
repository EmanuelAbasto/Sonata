using AudioUploader.Application.Configuration;
using AudioUploader.Infrastructure.Data;
using AudioUploader.Infrastructure.Options;
using AudioUploader.Web.Hubs;
using AudioUploader.Web.Middleware;
using AudioUploader.Web.Startup;
using DotNetEnv;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
string baseDirectory = AppContext.BaseDirectory;
string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
string currentDirectory = Directory.GetCurrentDirectory();

string[] envPaths = new[]
{
    Path.Combine(currentDirectory, ".env"),
    Path.Combine(projectRoot, ".env"),
    environment == "Development" ? ".env" : $".env.{environment}"
};

foreach (string path in envPaths)
{
    if (File.Exists(path))
    {
        Env.Load(path);
        break;
    }
}

string GetRequiredEnv(string key)
{
    string? value = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"ERROR CRÍTICO: La variable de entorno '{key}' no está definida. La aplicación no puede iniciar.");
    return value;
}

AppSettings settings = new AppSettings();

// Database
settings.Database.Host = GetRequiredEnv("DB_HOST");
settings.Database.Port = GetRequiredEnv("DB_PORT");
settings.Database.Name = GetRequiredEnv("DB_NAME");
settings.Database.User = GetRequiredEnv("DB_USER");
settings.Database.Password = GetRequiredEnv("DB_PASSWORD");

// MinIO
settings.Minio.Endpoint = GetRequiredEnv("MINIO_ENDPOINT");
settings.Minio.PublicEndpoint = Environment.GetEnvironmentVariable("MINIO_PUBLIC_ENDPOINT") ?? settings.Minio.Endpoint;
settings.Minio.AccessKey = GetRequiredEnv("MINIO_ACCESS_KEY");
settings.Minio.SecretKey = GetRequiredEnv("MINIO_SECRET_KEY");
settings.Minio.Bucket = GetRequiredEnv("MINIO_BUCKET");
settings.Minio.UseSsl = bool.Parse(GetRequiredEnv("MINIO_USE_SSL"));

// Procesamiento
settings.Ffmpeg.Path = GetRequiredEnv("FFMPEG_PATH");
settings.Whisper.PythonPath = GetRequiredEnv("PYTHON_PATH");
settings.Whisper.Model = GetRequiredEnv("WHISPER_MODEL");

settings.Summary.PythonPath = GetRequiredEnv("PYTHON_PATH");
settings.Summary.Model = GetRequiredEnv("SUMMARY_MODEL");
settings.Summary.OllamaUrl = GetRequiredEnv("OLLAMA_URL");
settings.Summary.MaxLength = int.Parse(GetRequiredEnv("SUMMARY_MAX_LENGTH"));
settings.Summary.MinLength = int.Parse(GetRequiredEnv("SUMMARY_MIN_LENGTH"));

// Batch y concurrencia
settings.Processing.BatchSize = int.Parse(GetRequiredEnv("BATCH_SIZE"));
settings.Processing.BatchIntervalSeconds = int.Parse(GetRequiredEnv("BATCH_INTERVAL_SECONDS"));
settings.Processing.MaxConcurrentCompressions = int.Parse(GetRequiredEnv("MAX_CONCURRENT_COMPRESSIONS"));

// RabbitMQ
RabbitMqOptions rabbitMqOptions = new RabbitMqOptions
{
    Host = GetRequiredEnv("RABBITMQ_HOST"),
    Port = int.Parse(GetRequiredEnv("RABBITMQ_PORT")),
    User = GetRequiredEnv("RABBITMQ_USER"),
    Password = GetRequiredEnv("RABBITMQ_PASSWORD"),
    VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/",
    TranscriptionResultsQueue = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_QUEUE") ?? "transcription-results",
    SummaryResultsQueue = Environment.GetEnvironmentVariable("RABBITMQ_SUMMARY_QUEUE") ?? "summary-results"
};

// Filtro de audio
AudioFilterOptions audioFilterOptions = new AudioFilterOptions
{
    FfmpegPath = settings.Ffmpeg.Path
};
string? configuredFilterChain = Environment.GetEnvironmentVariable("AUDIO_FILTER_CHAIN");
if (!string.IsNullOrWhiteSpace(configuredFilterChain))
{
    audioFilterOptions.FilterChain = configuredFilterChain;
}

settings.HostUrl = GetRequiredEnv("ASPNETCORE_URLS");

string ResolveScriptPath(string? configuredPath, string scriptName)
{
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        string fullPath = Path.GetFullPath(Path.Combine(currentDirectory, configuredPath));
        if (File.Exists(fullPath)) return fullPath;

        fullPath = Path.GetFullPath(Path.Combine(projectRoot, configuredPath));
        if (File.Exists(fullPath)) return fullPath;

        if (File.Exists(configuredPath)) return configuredPath;
    }

    string[] possiblePaths = new[]
    {
        Path.Combine(projectRoot, "src", "scripts", scriptName),
        Path.Combine(projectRoot, "scripts", scriptName),
        Path.Combine(currentDirectory, "scripts", scriptName),
        Path.Combine(AppContext.BaseDirectory, "scripts", scriptName)
    };

    foreach (string path in possiblePaths)
        if (File.Exists(path)) return path;

    return Path.Combine(projectRoot, "src", "scripts", scriptName);
}

settings.Whisper.ScriptPath = ResolveScriptPath(
    Environment.GetEnvironmentVariable("WHISPER_SCRIPT_PATH"),
    "whisper_transcribe.py");

settings.Summary.ScriptPath = ResolveScriptPath(
    Environment.GetEnvironmentVariable("SUMMARY_SCRIPT_PATH"),
    "summary_generator.py");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024;
    options.MemoryBufferThreshold = int.MaxValue;
});

string connectionString = $"Host={settings.Database.Host};Port={settings.Database.Port};Database={settings.Database.Name};Username={settings.Database.User};Password={settings.Database.Password}";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddApplicationConfiguration(settings, rabbitMqOptions, audioFilterOptions);

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddBackgroundServices();
builder.Services.AddMessaging();
builder.Services.AddCorsPolicy();
builder.Services.AddSwagger();
builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("AllowFrontend");
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audio Uploader API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles();
app.MapControllers();
app.MapHub<AudioProcessingHub>("/hubs/audio-processing");

app.Urls.Clear();
app.Urls.Add(settings.HostUrl);

ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started. Listening on: {HostUrl}", settings.HostUrl);
logger.LogInformation("Swagger: {SwaggerUrl}", $"{settings.HostUrl}/swagger");
logger.LogInformation("Whisper script: {WhisperPath}", settings.Whisper.ScriptPath);
logger.LogInformation("Whisper model: {WhisperModel}", settings.Whisper.Model);
logger.LogInformation("Summary script: {SummaryPath}", settings.Summary.ScriptPath);
logger.LogInformation("Summary model: {SummaryModel}", settings.Summary.Model);
logger.LogInformation("Summary max length: {MaxLength}, min length: {MinLength}",
    settings.Summary.MaxLength, settings.Summary.MinLength);
logger.LogInformation("Batch size: {BatchSize}, interval: {Interval}s",
    settings.Processing.BatchSize, settings.Processing.BatchIntervalSeconds);
logger.LogInformation("Max concurrent compressions: {MaxConcurrent}",
    settings.Processing.MaxConcurrentCompressions);
logger.LogInformation("RabbitMQ: {Host}:{Port} (vhost {VHost}), queues '{TranscriptionQueue}' / '{SummaryQueue}'",
    rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost,
    rabbitMqOptions.TranscriptionResultsQueue, rabbitMqOptions.SummaryResultsQueue);
logger.LogInformation("Audio filter chain: {FilterChain}", audioFilterOptions.FilterChain);

await app.RunAsync();