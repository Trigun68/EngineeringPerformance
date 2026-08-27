namespace EngineeringPerformance.Application.Monitoring;

/// <summary>Черновик метрики SQL до записи в БД.</summary>
public sealed class SqlCommandMetricDraft
{
    public DateTime TimestampUtc { get; init; }
    public string QueryType { get; init; } = "OTHER";
    public string? TablesInvolved { get; init; }
    public double DurationMs { get; init; }
    public int? RowsReturned { get; init; }
    public int? RowsAffected { get; init; }
    public string QueryHash { get; init; } = string.Empty;
    public string? SqlPreview { get; init; }
    public string? ParametersJson { get; init; }
}

public sealed record SqlSlowQueryDto(
    string QueryHash,
    string QueryType,
    string? TablesInvolved,
    string? SqlPreview,
    string? SampleEndpoint,
    int ExecutionCount,
    double AvgDurationMs,
    double MaxDurationMs,
    double P95DurationMs,
    double AvgRows);

public sealed record SqlFrequentQueryDto(
    string QueryHash,
    string QueryType,
    string? TablesInvolved,
    string? SqlPreview,
    string? SampleEndpoint,
    int ExecutionCount,
    double AvgDurationMs,
    double TotalDurationMs);

public sealed record SqlWeakTableDto(
    string TableName,
    int ExecutionCount,
    double AvgDurationMs,
    double MaxDurationMs,
    double TotalDurationMs,
    double AvgRows);

public sealed record SqlSizeCorrelationDto(
    string Bucket,
    int ExecutionCount,
    double AvgDurationMs,
    double AvgRows);

public sealed record SqlProfilingSummaryDto(
    int TotalCommands,
    double TotalSqlDurationMs,
    double AvgSqlDurationMs,
    double MaxSqlDurationMs,
    IReadOnlyList<SqlSlowQueryDto> TopSlow,
    IReadOnlyList<SqlFrequentQueryDto> TopFrequent,
    IReadOnlyList<SqlWeakTableDto> WeakTables,
    IReadOnlyList<SqlSizeCorrelationDto> SizeCorrelation);

public interface ISqlProfilingReportService
{
    Task<SqlProfilingSummaryDto> GetSummaryAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlSlowQueryDto>> GetSlowQueriesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlFrequentQueryDto>> GetFrequentQueriesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlWeakTableDto>> GetWeakTablesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default);
}

public interface ISqlProfilingPersistence
{
    Task PersistAsync(
        string endpoint,
        string? requestPath,
        string? correlationId,
        string? requestParametersJson,
        double requestDurationMs,
        IReadOnlyList<SqlCommandMetricDraft> commands,
        CancellationToken cancellationToken = default);
}
