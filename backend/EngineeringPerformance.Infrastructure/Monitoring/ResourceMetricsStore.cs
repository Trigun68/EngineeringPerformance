using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ResourceMetricsStore
{
    private readonly object _sync = new();
    private readonly List<ResourceSample> _samples = [];
    private TimeSpan _retention = TimeSpan.FromMinutes(60);

    public void ConfigureRetention(TimeSpan retention)
        => _retention = retention <= TimeSpan.Zero ? TimeSpan.FromMinutes(60) : retention;

    public void Add(ResourceSample sample)
    {
        lock (_sync)
        {
            PruneLocked(sample.TimestampUtc);
            _samples.Add(sample);
        }
    }

    public IReadOnlyList<ResourceSample> GetWindow(int windowMinutes, DateTime nowUtc)
    {
        var clamped = Math.Clamp(windowMinutes, 1, 360);
        var from = nowUtc.AddMinutes(-clamped);
        lock (_sync)
        {
            PruneLocked(nowUtc);
            return _samples.Where(s => s.TimestampUtc >= from).ToList();
        }
    }

    private void PruneLocked(DateTime nowUtc)
    {
        var cutoff = nowUtc - _retention;
        _samples.RemoveAll(s => s.TimestampUtc < cutoff);
    }

    internal static IReadOnlyList<MetricsTimeSeriesPointDto> Map(
        IReadOnlyList<ResourceSample> samples,
        Func<ResourceSample, double> selector)
        => samples
            .Select(s => new MetricsTimeSeriesPointDto(s.TimestampUtc.ToString("o"), selector(s)))
            .ToList();
}
