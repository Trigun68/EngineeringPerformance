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
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SqlProfilingOptions _options;
    private readonly ILogger<SqlProfilingPersistence> _logger;
    private DateTime _lastCleanupUtc = DateTime.MinValue;

    public SqlProfilingPersistence(
        IDbContextFactory<AppDbContext> dbFactory,
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

        // Не профилируем собственную запись метрик.
        var previous = SqlProfilingScope.Current;
        if (previous is not null)
            previous.SuppressPersistence = true;

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var rows = commands.Select(c => new SqlQueryMetric
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
            await context.SaveChangesAsync(cancellationToken);

            await MaybeCleanupAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist SQL profiling metrics ({Count} commands)", commands.Count);
        }
        finally
        {
            if (previous is not null)
                previous.SuppressPersistence = false;
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
