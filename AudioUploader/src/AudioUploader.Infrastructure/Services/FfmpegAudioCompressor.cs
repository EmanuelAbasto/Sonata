using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.Ports;
using AudioUploader.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioUploader.Infrastructure.Services
{
    public class FfmpegAudioCompressor : IAudioCompressor
    {
        private readonly FfmpegOptions _options;
        private readonly ILogger<FfmpegAudioCompressor> _logger;

        public FfmpegAudioCompressor(IOptions<FfmpegOptions> options, ILogger<FfmpegAudioCompressor> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<CompressionResult> CompressAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(inputPath))
            {
                return new CompressionResult
                {
                    Success = false,
                    Error = $"Input file not found: {inputPath}"
                };
            }

            string ffmpegPath = _options.FfmpegPath;
            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                ffmpegPath = "ffmpeg";
            }

            string arguments = $"-i \"{inputPath}\" -c:a aac -b:a 128k -movflags +faststart \"{outputPath}\" -y";

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
                using Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                string errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg error: {Error}", errorOutput);
                    return new CompressionResult
                    {
                        Success = false,
                        Error = $"FFmpeg exited with code {process.ExitCode}: {errorOutput}"
                    };
                }

                if (!File.Exists(outputPath))
                {
                    return new CompressionResult
                    {
                        Success = false,
                        Error = "Output file was not created"
                    };
                }

                FileInfo outputInfo = new FileInfo(outputPath);
                decimal? duration = await GetAudioDurationAsync(outputPath, cancellationToken);

                return new CompressionResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    FileSize = outputInfo.Length,
                    DurationSeconds = duration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during compression");
                return new CompressionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<decimal?> GetAudioDurationAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                string ffmpegPath = _options.FfmpegPath;
                if (string.IsNullOrWhiteSpace(ffmpegPath))
                {
                    ffmpegPath = "ffmpeg";
                }

                string arguments = $"-i \"{filePath}\" 2>&1";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = new Process();
                process.StartInfo = startInfo;
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