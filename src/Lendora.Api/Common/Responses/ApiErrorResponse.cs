namespace Lendora.Api.Common.Responses;

public class ApiErrorResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = [];
}
