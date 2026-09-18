using System.Text.Json.Serialization;

namespace AudioUploader.Application.DTOs
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("response")]
        public T? Response { get; set; }

        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(int statusCode, T response)
        {
            StatusCode = statusCode;
            Response = response;
        }

        public ApiResponse(int statusCode, string errorCode, string errorMessage, object? details = null)
        {
            StatusCode = statusCode;
            Error = new ApiError
            {
                Code = errorCode,
                Message = errorMessage,
                Details = details
            };
        }
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public object? Details { get; set; }
    }
}