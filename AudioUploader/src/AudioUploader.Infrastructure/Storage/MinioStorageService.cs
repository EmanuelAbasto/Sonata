using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using AudioUploader.Application.Configuration;
using AudioUploader.Application.Ports;

namespace AudioUploader.Infrastructure.Storage
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _minioSettings;

        public MinioStorageService(AppSettings appSettings)
        {
            _minioSettings = appSettings.Minio;

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            _minioClient = new MinioClient()
                .WithEndpoint(_minioSettings.Endpoint)
                .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                .WithSSL(_minioSettings.UseSsl)
                .WithHttpClient(httpClient)
                .Build();
        }

        public async Task UploadFileAsync(
            string bucketName,
            string objectName,
            Stream dataStream,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            bool bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucketName), cancellationToken);

            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
            }

            dataStream.Position = 0;

            PutObjectArgs args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(dataStream)
                .WithObjectSize(dataStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(args, cancellationToken);
        }

        public async Task<Stream> DownloadFileAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            MemoryStream memoryStream = new MemoryStream();

            GetObjectArgs args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(args, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> FileExistsAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                StatObjectArgs args = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);

                await _minioClient.StatObjectAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetPresignedUrlAsync(
            string bucketName,
            string objectName,
            int expiryInSeconds = 3600)
        {
            PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds);

            string internalUrl = await _minioClient.PresignedGetObjectAsync(args);

            string publicEndpoint = _minioSettings.PublicEndpoint;
            if (string.IsNullOrEmpty(publicEndpoint) || publicEndpoint == _minioSettings.Endpoint)
                return internalUrl;

            Uri internalUri = new Uri(internalUrl);

            if (publicEndpoint.StartsWith('/'))
            {
                string path = internalUri.AbsolutePath;
                string query = internalUri.Query;
                return publicEndpoint + path + query;
            }
            else
            {
                UriBuilder builder = new UriBuilder(publicEndpoint);
                builder.Path = internalUri.AbsolutePath;
                builder.Query = internalUri.Query.TrimStart('?');
                return builder.Uri.ToString();
            }
        }

        public async Task DeleteFileAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            RemoveObjectArgs args = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(args, cancellationToken);
        }

        public string GenerateObjectName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            string uniqueId = Guid.NewGuid().ToString("N");
            return $"uploads/{uniqueId}{extension}";
        }

        public string GetBucketName()
        {
            return _minioSettings.Bucket;
        }
    }
}