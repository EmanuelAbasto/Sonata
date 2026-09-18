namespace AudioUploader.Application.DTOs
{
    public class AudioDetailResponse : AudioFileDto
    {
        public AudioFileDto? LightFile { get; set; }
        public AudioFileDto? FilteredFile { get; set; }
    }
}