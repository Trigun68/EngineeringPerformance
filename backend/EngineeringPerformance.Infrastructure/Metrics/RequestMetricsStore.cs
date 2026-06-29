namespace EngineeringPerformance.Infrastructure.Metrics;

using EngineeringPerformance.Application.Monitoring;

/// <summary>
/// In-memory хранилище метрик RED для API (скользящее окно 24 ч).
/// </summary>
public sealed class RequestMetricsStore : IRequestMetricsStore
{
    private readonly object _sync = new();
    private readonly List<RequestMetricEntry> _entries = [];

    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public void Record(DateTime timestampUtc, double durationMs, int statusCode, string? exceptionType)
    {
        lock (_sync)
        {
            PruneLocked(timestampUtc);
            _entries.Add(new RequestMetricEntry(timestampUtc, durationMs, statusCode, exceptionType));
        }
    }

    public SystemMetricsSnapshot GetSnapshot(int rateWindowMinutes)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-rateWindowMinutes);
        var trendStart = now.AddHours(-24);

        lock (_sync)
        {
            PruneLocked(now);
            var windowEntries = _entries.Where(e => e.TimestampUtc >= windowStart).ToList();
            var trendEntries = _entries.Where(e => e.TimestampUtc >= trendStart).ToList();

            var totalRequests = windowEntries.Count;
            var avgDuration = totalRequests > 0
                ? windowEntries.Average(e => e.DurationMs)
                : 0d;
            var p95Duration = totalRequests > 0
                ? CalculatePercentile(windowEntries.Select(e => e.DurationMs), 0.95)
                : 0d;
            var serverErrors = windowEntries.Count(e => e.StatusCode >= 500);

            var requestRateSeries = BuildMinuteSeries(windowEntries, windowStart, now);
            var responseTrend = BuildHourlyAverageSeries(trendEntries, trendStart, now);
            var errorDistribution = BuildErrorDistribution(windowEntries);

            var red = new SystemMetricsRedDto(
                totalRequests,
                Math.Round(avgDuration, 2),
                Math.Round(p95Duration, 2),
                serverErrors,
                rateWindowMinutes);

            return new SystemMetricsSnapshot(red, requestRateSeries, errorDistribution, responseTrend);
        }
    }

    private void PruneLocked(DateTime nowUtc)
    {
        var cutoff = nowUtc - Retention;
        _entries.RemoveAll(e => e.TimestampUtc < cutoff);
    }

    private static IReadOnlyList<MetricsTimeSeriesPointDto> BuildMinuteSeries(
        IReadOnlyList<RequestMetricEntry> entries,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var buckets = new Dictionary<DateTime, int>();
        var cursor = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, fromUtc.Minute, 0, DateTimeKind.Utc);
        while (cursor <= toUtc)
        {
            buckets[cursor] = 0;
            cursor = cursor.AddMinutes(1);
        }

        foreach (var entry in entries)
        {
            var bucket = new DateTime(
                entry.TimestampUtc.Year,
                entry.TimestampUtc.Month,
                entry.TimestampUtc.Day,
                entry.TimestampUtc.Hour,
                entry.TimestampUtc.Minute,
                0,
                DateTimeKind.Utc);
            if (buckets.ContainsKey(bucket))
            {
                buckets[bucket]++;
            }
        }

        return buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new MetricsTimeSeriesPointDto(kv.Key.ToString("o"), kv.Value))
            .ToList();
    }

    private static IReadOnlyList<MetricsTimeSeriesPointDto> BuildHourlyAverageSeries(
        IReadOnlyList<RequestMetricEntry> entries,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var grouped = entries
            .GroupBy(e => new DateTime(
                e.TimestampUtc.Year,
                e.TimestampUtc.Month,
                e.TimestampUtc.Day,
                e.TimestampUtc.Hour,
                0,
                0,
                DateTimeKind.Utc))
            .ToDictionary(g => g.Key, g => g.Average(x => x.DurationMs));

        var result = new List<MetricsTimeSeriesPointDto>();
        var cursor = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, 0, 0, DateTimeKind.Utc);
        while (cursor <= toUtc)
        {
            grouped.TryGetValue(cursor, out var avg);
            result.Add(new MetricsTimeSeriesPointDto(cursor.ToString("o"), Math.Round(avg, 2)));
            cursor = cursor.AddHours(1);
        }

        return result;
    }

    private static IReadOnlyList<ErrorDistributionItemDto> BuildErrorDistribution(
        IReadOnlyList<RequestMetricEntry> entries)
    {
        return entries
            .Where(e => e.StatusCode >= 400)
            .GroupBy(e => ResolveErrorLabel(e))
            .Select(g => new ErrorDistributionItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private static string ResolveErrorLabel(RequestMetricEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ExceptionType))
        {
            return entry.ExceptionType;
        }

        return entry.StatusCode switch
        {
            >= 500 => $"HTTP {entry.StatusCode}",
            404 => "NotFound",
            403 => "Forbidden",
            401 => "Unauthorized",
            429 => "TooManyRequests",
            _ => $"HTTP {entry.StatusCode}",
        };
    }

    private static double CalculatePercentile(IEnumerable<double> values, double percentile)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        if (ordered.Length == 0)
            return 0d;

        if (ordered.Length == 1)
            return ordered[0];

        var clampedPercentile = Math.Clamp(percentile, 0d, 1d);
        var index = (ordered.Length - 1) * clampedPercentile;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return ordered[lower];

        var weight = index - lower;
        return ordered[lower] + ((ordered[upper] - ordered[lower]) * weight);
    }

    private sealed record RequestMetricEntry(
        DateTime TimestampUtc,
        double DurationMs,
        int StatusCode,
        string? ExceptionType);
}
