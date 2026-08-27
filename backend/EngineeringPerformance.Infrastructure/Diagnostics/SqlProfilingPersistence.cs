using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Diagnostics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Diagnostics;

public sealed class SqlProfilingPersistence : ISqlProfilingPersistence
{
    private readonly IAnalyticsDbContextFactory _dbFactory;
    private readonly SqlProfilingOptions _options;
    private readonly ILogger<SqlProfilingPersistence> _logger;
    private DateTime _lastCleanupUtc = DateTime.MinValue;

    public SqlProfilingPersistence(
        IAnalyticsDbContextFactory dbFactory,
        IOptions<SqlProfilingOptions> options,
        ILogger<SqlProfilingPersistence> logger)
    {
        _dbFactory = dbFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PersistAsync(
        string endpoint,
        string? requestPath,
        string? correlationId,
        string? requestParametersJson,
        double requestDurationMs,
        IReadOnlyList<SqlCommandMetricDraft> commands,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || commands.Count == 0)
            return;

        var threshold = _options.MinDurationMsToPersist;
        var filtered = threshold <= 0
            ? commands
            : commands.Where(c => c.DurationMs >= threshold).ToList();
        if (filtered.Count == 0)
            return;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var persistTimeout = Math.Clamp(_options.PersistCommandTimeoutSeconds, 1, 30);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(persistTimeout));
        var persistToken = timeoutCts.Token;

        var persistScope = SqlProfilingScope.Begin("sql-profiling-persist", null, correlationId, null);
        persistScope.SuppressPersistence = true;

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync(persistToken);
            context.Database.SetCommandTimeout(persistTimeout);
            var rows = filtered.Select(c => new SqlQueryMetric
            {
                TimestampUtc = c.TimestampUtc,
                Endpoint = Truncate(endpoint, 512) ?? "unknown",
                RequestPath = Truncate(requestPath, 512),
                QueryType = Truncate(c.QueryType, 16) ?? "OTHER",
                TablesInvolved = Truncate(c.TablesInvolved, 1000),
                DurationMs = c.DurationMs,
                RowsReturned = c.RowsReturned,
                RowsAffected = c.RowsAffected,
                QueryHash = c.QueryHash,
                SqlPreview = Truncate(c.SqlPreview, _options.MaxSqlPreviewLength),
                ParametersJson = Truncate(c.ParametersJson, _options.MaxParametersLength),
                RequestParametersJson = Truncate(requestParametersJson, 2000),
                RequestDurationMs = requestDurationMs,
                CorrelationId = Truncate(correlationId, 64),
            }).ToList();

            context.SqlQueryMetrics.AddRange(rows);
            await context.SaveChangesAsync(persistToken);

            await MaybeCleanupAsync(context, persistToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "SQL profiling persist timed out after {Timeout}s ({Count} commands)",
                persistTimeout,
                filtered.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist SQL profiling metrics ({Count} commands)", filtered.Count);
        }
        finally
        {
            SqlProfilingScope.End();
        }
    }

    private async Task MaybeCleanupAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanupUtc < TimeSpan.FromHours(6))
            return;

        _lastCleanupUtc = now;
        var cutoff = now.AddDays(-Math.Max(1, _options.RetentionDays));
        await context.SqlQueryMetrics
            .Where(x => x.TimestampUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null)
            return null;
        return value.Length <= max ? value : value[..max];
    }
}
