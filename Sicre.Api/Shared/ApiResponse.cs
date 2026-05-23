using System.Net;

namespace Sicre.Api.Shared;

public class ApiResponse<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = [];
    public HttpStatusCode StatusCode { get; private set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Data = data,
            StatusCode = HttpStatusCode.OK,
            Message = message,
        };

    public static ApiResponse<T> Fail(HttpStatusCode statusCode, string message) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
        };

    public static ApiResponse<T> Fail(HttpStatusCode statusCode, List<string> errors) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Errors = errors,
        };
}
