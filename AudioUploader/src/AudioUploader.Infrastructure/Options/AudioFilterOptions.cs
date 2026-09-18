namespace AudioUploader.Infrastructure.Options
{
    public class AudioFilterOptions
    {
        public string FfmpegPath { get; set; } = "ffmpeg";
        public string FilterChain { get; set; } = "afftdn=nf=-25,loudnorm=I=-16:TP=-1.5:LRA=11";
    }
}