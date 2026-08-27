using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Diagnostics;

public sealed class SqlProfilingReportService(IDbContextFactory<AppDbContext> dbFactory) : ISqlProfilingReportService
{
    private sealed record Row(
        string QueryHash,
        string QueryType,
        string? TablesInvolved,
        string? SqlPreview,
        string Endpoint,
        double DurationMs,
        double Rows);

    public async Task<SqlProfilingSummaryDto> GetSummaryAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        hours = Math.Clamp(hours, 1, 24 * 30);
        take = Math.Clamp(take, 1, 100);

        await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);
        var from = DateTime.UtcNow.AddHours(-hours);
        var raw = await context.SqlQueryMetrics.AsNoTracking()
            .Where(x => x.TimestampUtc >= from)
            .Select(x => new
            {
                x.QueryHash,
                x.QueryType,
                x.TablesInvolved,
                x.SqlPreview,
                x.Endpoint,
                x.DurationMs,
                Rows = (double)(x.RowsReturned ?? x.RowsAffected ?? 0),
            })
            .ToListAsync(cancellationToken);

        var rows = raw
            .Select(x => new Row(x.QueryHash, x.QueryType, x.TablesInvolved, x.SqlPreview, x.Endpoint, x.DurationMs, x.Rows))
            .ToList();

        if (rows.Count == 0)
        {
            return new SqlProfilingSummaryDto(0, 0, 0, 0, [], [], [], []);
        }

        var totalSql = rows.Sum(x => x.DurationMs);
        return new SqlProfilingSummaryDto(
            rows.Count,
            Math.Round(totalSql, 2),
            Math.Round(totalSql / rows.Count, 2),
            Math.Round(rows.Max(x => x.DurationMs), 2),
            BuildSlow(rows, take),
            BuildFrequent(rows, take),
            BuildWeakTables(rows, take),
            BuildSizeCorrelation(rows));
    }

    public async Task<IReadOnlyList<SqlSlowQueryDto>> GetSlowQueriesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default)
        => (await GetSummaryAsync(hours, take, cancellationToken)).TopSlow;

    public async Task<IReadOnlyList<SqlFrequentQueryDto>> GetFrequentQueriesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default)
        => (await GetSummaryAsync(hours, take, cancellationToken)).TopFrequent;

    public async Task<IReadOnlyList<SqlWeakTableDto>> GetWeakTablesAsync(
        int hours = 24,
        int take = 20,
        CancellationToken cancellationToken = default)
        => (await GetSummaryAsync(hours, take, cancellationToken)).WeakTables;

    private static IReadOnlyList<SqlSlowQueryDto> BuildSlow(IReadOnlyList<Row> rows, int take) =>
        rows
            .GroupBy(x => x.QueryHash)
            .Select(g =>
            {
                var durations = g.Select(x => x.DurationMs).OrderBy(x => x).ToArray();
                var first = g.First();
                return new SqlSlowQueryDto(
                    g.Key,
                    first.QueryType,
                    first.TablesInvolved,
                    first.SqlPreview,
                    g.GroupBy(x => x.Endpoint).OrderByDescending(x => x.Count()).First().Key,
                    g.Count(),
                    Math.Round(g.Average(x => x.DurationMs), 2),
                    Math.Round(g.Max(x => x.DurationMs), 2),
                    Math.Round(Percentile(durations, 0.95), 2),
                    Math.Round(g.Average(x => x.Rows), 2));
            })
            .OrderByDescending(x => x.P95DurationMs)
            .ThenByDescending(x => x.AvgDurationMs)
            .Take(take)
            .ToList();

    private static IReadOnlyList<SqlFrequentQueryDto> BuildFrequent(IReadOnlyList<Row> rows, int take) =>
        rows
            .GroupBy(x => x.QueryHash)
            .Select(g =>
            {
                var first = g.First();
                return new SqlFrequentQueryDto(
                    g.Key,
                    first.QueryType,
                    first.TablesInvolved,
                    first.SqlPreview,
                    g.GroupBy(x => x.Endpoint).OrderByDescending(x => x.Count()).First().Key,
                    g.Count(),
                    Math.Round(g.Average(x => x.DurationMs), 2),
                    Math.Round(g.Sum(x => x.DurationMs), 2));
            })
            .OrderByDescending(x => x.ExecutionCount)
            .ThenByDescending(x => x.TotalDurationMs)
            .Take(take)
            .ToList();

    private static IReadOnlyList<SqlWeakTableDto> BuildWeakTables(IReadOnlyList<Row> rows, int take) =>
        rows
            .Where(x => !string.IsNullOrWhiteSpace(x.TablesInvolved))
            .SelectMany(x => x.TablesInvolved!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => (Table: t, x.DurationMs, x.Rows)))
            .GroupBy(x => x.Table, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SqlWeakTableDto(
                g.Key,
                g.Count(),
                Math.Round(g.Average(x => x.DurationMs), 2),
                Math.Round(g.Max(x => x.DurationMs), 2),
                Math.Round(g.Sum(x => x.DurationMs), 2),
                Math.Round(g.Average(x => x.Rows), 2)))
            .OrderByDescending(x => x.TotalDurationMs)
            .Take(take)
            .ToList();

    private static IReadOnlyList<SqlSizeCorrelationDto> BuildSizeCorrelation(IReadOnlyList<Row> rows)
    {
        static string Bucket(double r) => r switch
        {
            <= 0 => "0",
            <= 10 => "1-10",
            <= 100 => "11-100",
            <= 1000 => "101-1000",
            <= 10000 => "1001-10000",
            _ => "10000+",
        };

        return rows
            .GroupBy(x => Bucket(x.Rows))
            .Select(g => new SqlSizeCorrelationDto(
                g.Key,
                g.Count(),
                Math.Round(g.Average(x => x.DurationMs), 2),
                Math.Round(g.Average(x => x.Rows), 2)))
            .OrderBy(x => x.Bucket switch
            {
                "0" => 0,
                "1-10" => 1,
                "11-100" => 2,
                "101-1000" => 3,
                "1001-10000" => 4,
                _ => 5,
            })
            .ToList();
    }

    private static double Percentile(double[] ordered, double p)
    {
        if (ordered.Length == 0)
            return 0;
        if (ordered.Length == 1)
            return ordered[0];

        var idx = (ordered.Length - 1) * Math.Clamp(p, 0, 1);
        var lo = (int)Math.Floor(idx);
        var hi = (int)Math.Ceiling(idx);
        if (lo == hi)
            return ordered[lo];

        return ordered[lo] + ((ordered[hi] - ordered[lo]) * (idx - lo));
    }
}
