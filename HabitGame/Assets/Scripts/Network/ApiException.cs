using System;
using Newtonsoft.Json;

public sealed class ApiException : Exception
{
    public long StatusCode { get; }

    private ApiException(long statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public static ApiException FromResponse(long statusCode, string requestError, string responseText)
    {
        string backendMessage = null;

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                backendMessage = JsonConvert.DeserializeObject<ApiErrorResponse>(responseText)?.Message;
            }
            catch (JsonException)
            {
                // A non-JSON error body is not exposed directly to the player.
            }
        }

        string detail = !string.IsNullOrWhiteSpace(backendMessage)
            ? backendMessage
            : requestError;

        if (string.IsNullOrWhiteSpace(detail))
            detail = "서버 요청에 실패했습니다.";

        return new ApiException(statusCode, $"HTTP {statusCode}: {detail}");
    }

    private sealed class ApiErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
