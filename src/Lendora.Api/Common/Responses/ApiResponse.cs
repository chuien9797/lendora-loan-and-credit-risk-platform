namespace Lendora.Api.Common.Responses;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string message, string? traceId = null) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId
        };
}
