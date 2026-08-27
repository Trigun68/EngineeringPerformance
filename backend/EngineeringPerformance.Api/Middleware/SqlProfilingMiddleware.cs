using System.Diagnostics;
using System.Text.Json;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// End-to-end замер HTTP + flush SQL-метрик, собранных DbCommandInterceptor.
/// </summary>
public sealed class SqlProfilingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<SqlProfilingOptions> _options;

    public SqlProfilingMiddleware(RequestDelegate next, IOptions<SqlProfilingOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, ISqlProfilingPersistence persistence)
    {
        if (!_options.Value.Enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value;
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid)
            ? cid?.ToString()
            : context.TraceIdentifier;
        var requestParams = CaptureRequestParameters(context);

        // Endpoint ещё не известен до routing — уточним после pipeline.
        var scope = SqlProfilingScope.Begin(
            $"{context.Request.Method} {path}",
            path,
            correlationId,
            requestParams);

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var endpoint = ResolveEndpoint(context);
            var commands = scope.Snapshot();
            // Снимаем HTTP-scope до persist: INSERT метрик идёт в собственном scope
            // с коротким timeout и не конкурирует с пользовательским SQL (30 с).
            scope.SuppressPersistence = true;
            SqlProfilingScope.End();
            if (commands.Count > 0)
            {
                _ = persistence.PersistAsync(
                    endpoint,
                    path,
                    correlationId,
                    requestParams,
                    sw.Elapsed.TotalMilliseconds,
                    commands,
                    CancellationToken.None);
            }
        }
    }

    private static string ResolveEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint
            && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return $"{context.Request.Method} /{routeEndpoint.RoutePattern.RawText.TrimStart('/')}";
        }

        if (endpoint?.DisplayName is { Length: > 0 } display)
            return display;

        return $"{context.Request.Method} {context.Request.Path.Value}";
    }

    private static string? CaptureRequestParameters(HttpContext context)
    {
        try
        {
            var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in context.Request.Query)
                payload[$"query.{kv.Key}"] = kv.Value.ToString();

            foreach (var kv in context.Request.RouteValues)
            {
                if (kv.Key.Equals("action", StringComparison.OrdinalIgnoreCase)
                    || kv.Key.Equals("controller", StringComparison.OrdinalIgnoreCase))
                    continue;
                payload[$"route.{kv.Key}"] = kv.Value?.ToString();
            }

            if (payload.Count == 0)
                return null;

            var json = JsonSerializer.Serialize(payload);
            return json.Length <= 2000 ? json : json[..2000];
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/api/monitoring/sql-metrics", StringComparison.OrdinalIgnoreCase);
    }
}
