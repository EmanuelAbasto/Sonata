using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IAudioCompressor
    {
        Task<CompressionResult> CompressAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);
    }

    public class CompressionResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public long? FileSize { get; set; }
        public string? Error { get; set; }
        public decimal? DurationSeconds { get; set; }
    }
}