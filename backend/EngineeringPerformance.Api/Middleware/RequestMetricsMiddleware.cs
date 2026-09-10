using System.Diagnostics;
using System.Security.Claims;
using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// Сбор метрик RED: rate, duration, errors для каждого HTTP-запроса.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMetricsStore _metricsStore;
    private readonly IActiveUserTracker _activeUsers;

    public RequestMetricsMiddleware(
        RequestDelegate next,
        IRequestMetricsStore metricsStore,
        IActiveUserTracker activeUsers)
    {
        _next = next;
        _metricsStore = metricsStore;
        _activeUsers = activeUsers;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var skipRed = MonitoringSelfTraffic.ExcludeFromRed(path);
        if (skipRed)
        {
            await _next(context);
            RecordPresence(context, path);
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

            RecordPresence(context, path);
        }
    }

    private void RecordPresence(HttpContext context, string? path)
    {
        if (MonitoringSelfTraffic.ExcludeFromPresence(path))
            return;

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idValue, out var userId))
            _activeUsers.Record(userId, DateTime.UtcNow);
    }
}
