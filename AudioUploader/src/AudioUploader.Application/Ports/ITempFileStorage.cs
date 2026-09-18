using System;

namespace AudioUploader.Application.Ports
{
    public interface ITempFileStorage
    {
        void Add(Guid jobId, string filePath);
        string Get(Guid jobId);
        void Remove(Guid jobId);
        bool Exists(Guid jobId);
    }
}