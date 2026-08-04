using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Diagnostics;

/// <summary>
/// AsyncLocal-контекст SQL-профилирования на время HTTP-запроса.
/// </summary>
public sealed class SqlProfilingScope
{
    private static readonly AsyncLocal<SqlProfilingScope?> CurrentLocal = new();

    private readonly List<SqlCommandMetricDraft> _commands = [];
    private readonly object _sync = new();

    public static SqlProfilingScope? Current => CurrentLocal.Value;

    public string Endpoint { get; }
    public string? RequestPath { get; }
    public string? CorrelationId { get; }
    public string? RequestParametersJson { get; }
    public bool SuppressPersistence { get; set; }

    private SqlProfilingScope(
        string endpoint,
        string? requestPath,
        string? correlationId,
        string? requestParametersJson)
    {
        Endpoint = endpoint;
        RequestPath = requestPath;
        CorrelationId = correlationId;
        RequestParametersJson = requestParametersJson;
    }

    public static SqlProfilingScope Begin(
        string endpoint,
        string? requestPath,
        string? correlationId,
        string? requestParametersJson)
    {
        var scope = new SqlProfilingScope(endpoint, requestPath, correlationId, requestParametersJson);
        CurrentLocal.Value = scope;
        return scope;
    }

    public static void End() => CurrentLocal.Value = null;

    public void Add(SqlCommandMetricDraft draft)
    {
        if (SuppressPersistence)
            return;

        lock (_sync)
            _commands.Add(draft);
    }

    public IReadOnlyList<SqlCommandMetricDraft> Snapshot()
    {
        lock (_sync)
            return _commands.ToList();
    }
}
