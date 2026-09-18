using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioUploader.Application.DTOs;

namespace AudioUploader.Application.Ports
{
    public interface IAudioService
    {
        Task<IEnumerable<UploadResponse>> UploadFilesAsync(
            IEnumerable<Stream> fileStreams,
            IEnumerable<string> fileNames,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AudioFileDto>> GetAudioFilesPagedAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? format = null,
            CancellationToken cancellationToken = default);

        Task<AudioDetailResponse?> GetAudioDetailAsync(
            int audioId,
            CancellationToken cancellationToken = default);
    }
}