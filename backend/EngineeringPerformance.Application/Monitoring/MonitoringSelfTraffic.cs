namespace EngineeringPerformance.Application.Monitoring;

public static class MonitoringSelfTraffic
{
    public static bool ExcludeFromRed(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/monitoring/resource-metrics", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ExcludeFromPresence(string? path)
    {
        if (ExcludeFromRed(path))
            return true;

        if (string.IsNullOrEmpty(path))
            return false;

        return path.Equals("/api/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/metrics", StringComparison.OrdinalIgnoreCase);
    }
}
