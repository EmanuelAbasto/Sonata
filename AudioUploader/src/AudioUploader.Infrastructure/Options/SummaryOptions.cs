namespace AudioUploader.Infrastructure.Options
{
    public class SummaryOptions
    {
        public string ScriptPath { get; set; }
        public string PythonPath { get; set; }
        public string Model { get; set; }
        public string OllamaUrl { get; set; }
        public int MaxLength { get; set; }
        public int MinLength { get; set; }
    }
}