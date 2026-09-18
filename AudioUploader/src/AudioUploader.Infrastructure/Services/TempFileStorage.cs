using System;
using System.Collections.Concurrent;
using AudioUploader.Application.Ports;

namespace AudioUploader.Infrastructure.Services
{
    public class TempFileStorage : ITempFileStorage
    {
        private readonly ConcurrentDictionary<Guid, string> _storage = new ConcurrentDictionary<Guid, string>();

        public void Add(Guid jobId, string filePath)
        {
            _storage.TryAdd(jobId, filePath);
        }

        public string Get(Guid jobId)
        {
            _storage.TryGetValue(jobId, out string filePath);
            return filePath;
        }

        public void Remove(Guid jobId)
        {
            _storage.TryRemove(jobId, out string _);
        }

        public bool Exists(Guid jobId)
        {
            return _storage.ContainsKey(jobId);
        }
    }
}