using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace TemplarCMS.Api.Observability;

public sealed class RequestObservabilityMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    private const int MaximumCorrelationIdLength = 128;

    private readonly RequestDelegate _next;

    public RequestObservabilityMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ILogger<RequestObservabilityMiddleware> logger)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[CorrelationIdHeaderName]);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        using (logger.BeginScope(
                   new Dictionary<string, object?>
                   {
                       ["CorrelationId"] = correlationId
                   }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath} completed with {StatusCode} in {ElapsedMilliseconds} ms.",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    private static string GetCorrelationId(StringValues requestHeader)
    {
        if (requestHeader.Count == 1 && IsValidCorrelationId(requestHeader[0]))
        {
            return requestHeader[0]!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumCorrelationIdLength)
        {
            return false;
        }

        return value.All(
            character =>
                char.IsAsciiLetterOrDigit(character)
                || character is '-' or '_' or '.');
    }
}

public static class RequestObservabilityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTemplarRequestObservability(
        this IApplicationBuilder application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.UseMiddleware<RequestObservabilityMiddleware>();
    }
}
