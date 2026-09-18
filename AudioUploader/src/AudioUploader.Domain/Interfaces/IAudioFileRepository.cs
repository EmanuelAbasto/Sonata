using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Domain.Entities;
using AudioUploader.Domain.Enums;

namespace AudioUploader.Domain.Interfaces
{
    public interface IAudioFileRepository
    {
        Task<AudioFile> AddAsync(AudioFile audioFile, CancellationToken cancellationToken = default);
        Task<AudioFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<AudioFile?> GetByBucketPathAsync(string bucketPath, CancellationToken cancellationToken = default);
        Task UpdateAsync(AudioFile audioFile, CancellationToken cancellationToken = default);
        Task<(IEnumerable<AudioFile> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? format = null,
            CancellationToken cancellationToken = default);
    }
}