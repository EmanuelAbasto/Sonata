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
    public class PythonSummaryService : ISummaryService
    {
        private readonly string _scriptPath;
        private readonly string _pythonPath;
        private readonly string _model;
        private readonly string _ollamaUrl;
        private readonly int _maxLength;
        private readonly int _minLength;
        private readonly ILogger<PythonSummaryService> _logger;

        public PythonSummaryService(
            AppSettings appSettings,
            ILogger<PythonSummaryService> logger)
        {
            _scriptPath = appSettings.Summary.ScriptPath;
            _pythonPath = appSettings.Summary.PythonPath;
            _model = appSettings.Summary.Model;
            _ollamaUrl = appSettings.Summary.OllamaUrl;
            _maxLength = appSettings.Summary.MaxLength;
            _minLength = appSettings.Summary.MinLength;
            _logger = logger;

            if (string.IsNullOrEmpty(_scriptPath) || !File.Exists(_scriptPath))
            {
                throw new InvalidOperationException($"Summary script not found at: '{_scriptPath}'. Ensure SUMMARY_SCRIPT_PATH environment variable is set correctly.");
            }
        }

        public async Task GenerateSummariesBatchAsync(IEnumerable<SummaryJobInput> jobs, CancellationToken cancellationToken = default)
        {
            List<SummaryJobInput> jobList = new List<SummaryJobInput>(jobs);
            if (jobList.Count == 0) return;

            string tempTextsFile = Path.GetTempFileName() + ".txt";

            try
            {
                await File.WriteAllLinesAsync(tempTextsFile, jobList.Select(j => j.Text.Replace('\n', ' ').Replace('\r', ' ')), cancellationToken);

                string jobIdsArg = string.Join(" ", jobList.Select(j => j.JobId.ToString()));

                string arguments =
                    $"\"{_scriptPath}\" " +
                    $"--model {_model} " +
                    $"--texts \"{tempTextsFile}\" " +
                    $"--job-ids {jobIdsArg} " +
                    $"--max-length {_maxLength} " +
                    $"--min-length {_minLength} " +
                    $"--ollama-url {_ollamaUrl}";

                _logger.LogInformation("Running summary batch of {Count} texts", jobList.Count);

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

                Task stdoutTask = DrainStreamAsync(process.StandardOutput, line => _logger.LogInformation("[Summary stdout] {Line}", line));
                Task stderrTask = DrainStreamAsync(process.StandardError, line => _logger.LogInformation("[Summary stderr] {Line}", line));

                int timeoutMs = 10 * 60 * 1000;
                Task waitTask = process.WaitForExitAsync(cancellationToken);

                if (await Task.WhenAny(waitTask, Task.Delay(timeoutMs, cancellationToken)) != waitTask)
                {
                    process.Kill(entireProcessTree: true);
                    throw new TimeoutException("Summary batch exceeded timeout (10 minutes).");
                }

                await waitTask;
                await Task.WhenAll(stdoutTask, stderrTask);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Summary script exited with code {ExitCode}", process.ExitCode);
                    throw new InvalidOperationException($"Summary script failed with exit code {process.ExitCode}");
                }
            }
            finally
            {
                if (File.Exists(tempTextsFile)) File.Delete(tempTextsFile);
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