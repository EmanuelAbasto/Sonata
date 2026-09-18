using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Application.Ports
{
    public interface IStorageService
    {
        Task UploadFileAsync(string bucketName, string objectName, Stream dataStream, string contentType, CancellationToken cancellationToken = default);
        Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
        Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600);
        Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
        string GenerateObjectName(string fileName);
        string GetBucketName();
    }
}