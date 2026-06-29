using Serilog.Context;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// Проставляет correlation id для каждого запроса и прокидывает его в лог-контекст/ответ.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationIdHeader]);
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return Guid.NewGuid().ToString("N");

        var sanitized = candidate.Trim();
        if (sanitized.Length is < 8 or > 64)
            return Guid.NewGuid().ToString("N");

        return sanitized;
    }
}
