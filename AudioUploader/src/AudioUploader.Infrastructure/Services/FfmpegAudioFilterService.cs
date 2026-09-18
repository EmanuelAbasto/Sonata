using AudioUploader.Application.Ports;
using AudioUploader.Domain.Entities;
using AudioUploader.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Infrastructure.Services
{
    public class FfmpegAudioFilterService : IAudioFilterService
    {
        private readonly AudioFilterOptions _options;
        private readonly ILogger<FfmpegAudioFilterService> _logger;

        public FfmpegAudioFilterService(IOptions<AudioFilterOptions> options, ILogger<FfmpegAudioFilterService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AudioFilterResult> ApplyFilterAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(inputPath))
            {
                return new AudioFilterResult { Success = false, Error = $"Input file not found: {inputPath}" };
            }

            string ffmpegPath = string.IsNullOrWhiteSpace(_options.FfmpegPath) ? "ffmpeg" : _options.FfmpegPath;
            string filterChain = _options.FilterChain;

            string arguments = $"-i \"{inputPath}\" -af \"{filterChain}\" -c:a aac -b:a 128k -movflags +faststart \"{outputPath}\" -y";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using Process process = new Process { StartInfo = startInfo };
                process.Start();

                string errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg filter error: {Error}", errorOutput);
                    return new AudioFilterResult
                    {
                        Success = false,
                        Error = $"FFmpeg exited with code {process.ExitCode}: {errorOutput}"
                    };
                }

                if (!File.Exists(outputPath))
                {
                    return new AudioFilterResult { Success = false, Error = "Output file was not created" };
                }

                FileInfo outputInfo = new FileInfo(outputPath);
                decimal? duration = await GetAudioDurationAsync(ffmpegPath, outputPath, cancellationToken);

                return new AudioFilterResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    FileSize = outputInfo.Length,
                    DurationSeconds = duration,
                    FilterChain = filterChain
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying audio filter");
                return new AudioFilterResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<decimal?> GetAudioDurationAsync(string ffmpegPath, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{filePath}\" 2>&1",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = new Process { StartInfo = startInfo };
                process.Start();

                string output = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                int durationIndex = output.IndexOf("Duration:", StringComparison.OrdinalIgnoreCase);
                if (durationIndex >= 0)
                {
                    string durationPart = output.Substring(durationIndex + 9, 11);
                    if (TimeSpan.TryParse(durationPart, out TimeSpan duration))
                    {
                        return (decimal)duration.TotalSeconds;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}