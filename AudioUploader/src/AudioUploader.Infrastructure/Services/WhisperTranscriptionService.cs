using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.Configuration;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Ports;
using Microsoft.Extensions.Logging;

namespace AudioUploader.Infrastructure.Services
{
    public class WhisperTranscriptionService : ITranscriptionService
    {
        private readonly string _scriptPath;
        private readonly string _pythonPath;
        private readonly string _model;
        private readonly ILogger<WhisperTranscriptionService> _logger;

        public WhisperTranscriptionService(
            AppSettings appSettings,
            ILogger<WhisperTranscriptionService> logger)
        {
            _scriptPath = appSettings.Whisper.ScriptPath;
            _pythonPath = appSettings.Whisper.PythonPath;
            _model = appSettings.Whisper.Model;
            _logger = logger;

            if (string.IsNullOrEmpty(_scriptPath) || !File.Exists(_scriptPath))
            {
                throw new InvalidOperationException($"Whisper script not found at: '{_scriptPath}'. Ensure WHISPER_SCRIPT_PATH environment variable is set correctly.");
            }
        }

        public async Task TranscribeBatchAsync(IEnumerable<TranscriptionJobInput> jobs, CancellationToken cancellationToken = default)
        {
            List<TranscriptionJobInput> jobList = new List<TranscriptionJobInput>(jobs);
            if (jobList.Count == 0) return;

            string filesArg = string.Join(" ", jobList.Select(j => $"\"{Path.GetFullPath(j.FilePath)}\""));
            string jobIdsArg = string.Join(" ", jobList.Select(j => j.JobId.ToString()));

            string arguments = $"\"{_scriptPath}\" --model {_model} --files {filesArg} --job-ids {jobIdsArg}";

            _logger.LogInformation("Running Whisper batch of {Count} files", jobList.Count);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            Task stdoutTask = DrainStreamAsync(process.StandardOutput, line => _logger.LogInformation("[Whisper stdout] {Line}", line));
            Task stderrTask = DrainStreamAsync(process.StandardError, line => _logger.LogInformation("[Whisper stderr] {Line}", line));

            int timeoutMs = 10 * 60 * 1000;
            Task waitTask = process.WaitForExitAsync(cancellationToken);

            if (await Task.WhenAny(waitTask, Task.Delay(timeoutMs, cancellationToken)) != waitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Transcription batch exceeded timeout (10 minutes).");
            }

            await waitTask;
            await Task.WhenAll(stdoutTask, stderrTask);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Whisper script exited with code {ExitCode}", process.ExitCode);
                throw new InvalidOperationException($"Whisper script failed with exit code {process.ExitCode}");
            }
        }

        private static async Task DrainStreamAsync(StreamReader reader, Action<string> onLine)
        {
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line)) onLine(line);
            }
        }
    }
}