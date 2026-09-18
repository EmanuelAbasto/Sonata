using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AudioUploader.Application.DTOs;
using AudioUploader.Application.Services;

namespace AudioUploader.Web.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    [Produces("application/json")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IJobService jobService, ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener estado de un job.
        /// </summary>
        [HttpGet("{jobId}/status")]
        [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            JobStatusResponse? status = await _jobService.GetJobStatusAsync(jobId, CancellationToken.None);

            if (status == null)
            {
                ApiResponse<object> notFound = new ApiResponse<object>(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    $"Job with ID {jobId} not found.");
                return NotFound(notFound);
            }

            ApiResponse<JobStatusResponse> response = new ApiResponse<JobStatusResponse>(
                StatusCodes.Status200OK,
                status);

            return Ok(response);
        }

        /// <summary>
        /// Obtener la transcripción de un job.
        /// </summary>
        [HttpGet("{jobId}/transcription")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTranscription(Guid jobId)
        {
            string? text = await _jobService.GetTranscriptionTextAsync(jobId, CancellationToken.None);

            if (text == null)
            {
                ApiResponse<object> notFound = new ApiResponse<object>(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    $"Transcription not found for job {jobId}.");
                return NotFound(notFound);
            }

            object responseObj = new { text };
            ApiResponse<object> response = new ApiResponse<object>(
                StatusCodes.Status200OK,
                responseObj);

            return Ok(response);
        }

        /// <summary>
        /// Obtener el resumen de un job.
        /// </summary>
        [HttpGet("{jobId}/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSummary(Guid jobId)
        {
            string? summary = await _jobService.GetSummaryAsync(jobId, CancellationToken.None);

            if (summary == null)
            {
                ApiResponse<object> notFound = new ApiResponse<object>(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    $"Summary not found for job {jobId}.");
                return NotFound(notFound);
            }

            object responseObj = new { summary };
            ApiResponse<object> response = new ApiResponse<object>(
                StatusCodes.Status200OK,
                responseObj);

            return Ok(response);
        }
    }
}