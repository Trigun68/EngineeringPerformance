using System.Diagnostics;
using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// Сбор метрик RED: rate, duration, errors для каждого HTTP-запроса.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMetricsStore _metricsStore;

    public RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsStore metricsStore)
    {
        _next = next;
        _metricsStore = metricsStore;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var exceptionType = context.Items.TryGetValue("ExceptionType", out var value)
                ? value?.ToString()
                : null;

            if (exceptionType is null && context.Response.StatusCode >= 500)
            {
                exceptionType = $"Http{context.Response.StatusCode}";
            }

            _metricsStore.Record(
                DateTime.UtcNow,
                stopwatch.Elapsed.TotalMilliseconds,
                context.Response.StatusCode,
                exceptionType);
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
    }
}
