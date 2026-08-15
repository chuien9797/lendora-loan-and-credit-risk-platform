using System.Net;
using System.Text.Json;
using Lendora.Api.Common.Responses;

namespace Lendora.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for request {Path}", context.Request.Path);
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = "An unexpected error occurred.",
            StatusCode = context.Response.StatusCode,
            TraceId = context.TraceIdentifier,
            Errors = [exception.Message]
        };

        var payload = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(payload);
    }
}
