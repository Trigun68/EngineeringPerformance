namespace EngineeringPerformance.Infrastructure.Monitoring;

internal sealed record RedisInfoSnapshot(
    long UsedMemoryBytes,
    long UsedMemoryRssBytes,
    int ConnectedClients,
    double UsedCpuSeconds,
    double InstantaneousOpsPerSec,
    long KeyspaceHits,
    long KeyspaceMisses,
    long EvictedKeys,
    double FragmentationRatio)
{
    public double HitRatio
    {
        get
        {
            var total = KeyspaceHits + KeyspaceMisses;
            return total <= 0 ? 0d : KeyspaceHits / (double)total;
        }
    }
}

internal static class RedisInfoParser
{
    public static RedisInfoSnapshot Parse(IReadOnlyDictionary<string, string> info)
    {
        var used = GetInt64(info, "used_memory");
        var rss = GetInt64(info, "used_memory_rss");
        if (rss <= 0)
            rss = used;

        return new RedisInfoSnapshot(
            used,
            rss,
            (int)GetInt64(info, "connected_clients"),
            GetDouble(info, "used_cpu_sys") + GetDouble(info, "used_cpu_user"),
            GetDouble(info, "instantaneous_ops_per_sec"),
            GetInt64(info, "keyspace_hits"),
            GetInt64(info, "keyspace_misses"),
            GetInt64(info, "evicted_keys"),
            GetDouble(info, "mem_fragmentation_ratio"));
    }

    public static RedisInfoSnapshot ParseRaw(string infoText)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in infoText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || !trimmed.Contains(':'))
                continue;

            var idx = trimmed.IndexOf(':');
            var key = trimmed[..idx];
            var value = trimmed[(idx + 1)..];
            map[key] = value;
        }

        return Parse(map);
    }

    private static long GetInt64(IReadOnlyDictionary<string, string> info, string key)
        => info.TryGetValue(key, out var raw) && long.TryParse(raw, out var value) ? value : 0;

    private static double GetDouble(IReadOnlyDictionary<string, string> info, string key)
        => info.TryGetValue(key, out var raw)
            && double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0d;
}
