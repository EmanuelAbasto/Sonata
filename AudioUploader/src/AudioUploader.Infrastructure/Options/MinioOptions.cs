namespace AudioUploader.Infrastructure.Options
{
    public class MinioOptions
    {
        public string Endpoint { get; set; }
        public string PublicEndpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public bool UseSsl { get; set; }
    }
}