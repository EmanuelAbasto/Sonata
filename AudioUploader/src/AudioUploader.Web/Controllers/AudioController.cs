using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Services;
using AudioUploader.Application.Ports;

namespace AudioUploader.Web.Controllers
{
    [ApiController]
    [Route("api/audio")]
    [Produces("application/json")]
    public class AudioController : ControllerBase
    {
        private readonly IAudioService _audioService;
        private readonly ILogger<AudioController> _logger;

        public AudioController(IAudioService audioService, ILogger<AudioController> logger)
        {
            _audioService = audioService;
            _logger = logger;
        }

        /// <summary>
        /// Subir uno o varios archivos de audio.
        /// </summary>
        /// <param name="files">Archivos de audio a subir</param>
        /// <returns>Lista de respuestas de subida</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UploadResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFiles(
            [FromForm] List<IFormFile> files)
        {
            CancellationToken cancellationToken = HttpContext.RequestAborted;
            if (files == null || files.Count == 0)
                return BadRequest(new ApiResponse<object>(400, "NO_FILES", "At least one file is required."));

            const long maxFileSize = 100L * 1024 * 1024;
            List<Stream> streams = new List<Stream>();
            List<string> fileNames = new List<string>();

            foreach (IFormFile file in files)
            {
                if (file.Length > maxFileSize)
                    return BadRequest(new ApiResponse<object>(400, "FILE_TOO_LARGE", $"File {file.FileName} exceeds 100 MB."));

                streams.Add(file.OpenReadStream());
                fileNames.Add(file.FileName);
            }

            try
            {
                IEnumerable<UploadResponse> responses = await _audioService.UploadFilesAsync(
                    streams,
                    fileNames,
                    cancellationToken);

                return Ok(new ApiResponse<IEnumerable<UploadResponse>>(200, responses));
            }
            finally
            {
                foreach (Stream stream in streams)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Listar archivos de audio con paginación y filtros.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AudioFileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAudioFiles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? format = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            PagedResult<AudioFileDto> result = await _audioService.GetAudioFilesPagedAsync(
                page, pageSize, searchTerm, status, fromDate, toDate, format, CancellationToken.None);

            ApiResponse<PagedResult<AudioFileDto>> response = new ApiResponse<PagedResult<AudioFileDto>>(
                StatusCodes.Status200OK,
                result);

            return Ok(response);
        }

        /// <summary>
        /// Obtener metadatos detallados de un archivo específico.
        /// </summary>
        [HttpGet("{audioId}")]
        [ProducesResponseType(typeof(ApiResponse<AudioDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAudioDetail(int audioId)
        {
            AudioDetailResponse? detail = await _audioService.GetAudioDetailAsync(audioId, CancellationToken.None);

            if (detail == null)
            {
                ApiResponse<object> notFound = new ApiResponse<object>(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    $"Audio file with ID {audioId} not found.");
                return NotFound(notFound);
            }

            ApiResponse<AudioDetailResponse> response = new ApiResponse<AudioDetailResponse>(
                StatusCodes.Status200OK,
                detail);

            return Ok(response);
        }
    }
}