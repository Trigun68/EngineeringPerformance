namespace EngineeringPerformance.Application.Monitoring;

public static class ResourceVerdictBuilder
{
    public static IReadOnlyList<string> Build(IReadOnlyList<ResourceVerdictInput> samples)
    {
        if (samples.Count == 0)
            return ["Сбор ещё не накопил точки: подождите 10–20 секунд."];

        var latest = samples[^1];
        var lines = new List<string>(4);

        AddShareLine(lines, "CPU", latest.BackendCpuCores, latest.RedisCpuCores, latest.SqlCpuCores, "ядра");
        AddShareLine(lines, "RAM", latest.BackendRssMib, latest.RedisRssMib, latest.SqlRssMib, "МиБ");

        if (latest.AnalyticsGateBusy >= latest.AnalyticsGateLimit && latest.AnalyticsGateLimit > 0)
            lines.Add("Аналитика в очереди: заняты оба слота AnalyticsSqlGate (page-data / экспорт / сравнение).");

        if (LooksLikeSqlConnectionLeak(samples))
            lines.Add("Подозрение на утечку SQL-коннектов: сессии растут при почти неизменном числе пользователей.");

        if (LooksLikeGcPressure(samples))
            lines.Add("Давление GC: скорость аллокаций или число Gen2 выбивается из медианы окна.");

        return lines;
    }

    private static void AddShareLine(
        List<string> lines,
        string resource,
        double backend,
        double redis,
        double sql,
        string unit)
    {
        var total = backend + redis + sql;
        if (total <= 0d)
            return;

        string Pct(double part) => $"{Math.Round(100d * part / total, 0)}%";
        lines.Add(
            $"{resource}: SQL {Pct(sql)} ({Format(sql)} {unit}), бэкенд {Pct(backend)} ({Format(backend)} {unit}), Redis {Pct(redis)} ({Format(redis)} {unit}).");
    }

    private static bool LooksLikeSqlConnectionLeak(IReadOnlyList<ResourceVerdictInput> samples)
    {
        if (samples.Count < 6)
            return false;

        var window = samples.TakeLast(6).ToArray();
        var sqlDelta = window[^1].SqlConnections - window[0].SqlConnections;
        var userDelta = Math.Abs(window[^1].ActiveUsers - window[0].ActiveUsers);
        return sqlDelta >= 3 && userDelta <= 1;
    }

    private static bool LooksLikeGcPressure(IReadOnlyList<ResourceVerdictInput> samples)
    {
        if (samples.Count < 5)
            return false;

        var rates = samples.Select(s => s.AllocRateBytesPerSecond).ToArray();
        var median = Median(rates[..^1]);
        var latestRate = rates[^1];
        if (median > 0 && latestRate > median * 2d && latestRate > 8 * 1024 * 1024)
            return true;

        var gen2Deltas = new List<int>(samples.Count - 1);
        for (var i = 1; i < samples.Count; i++)
            gen2Deltas.Add(Math.Max(0, samples[i].Gen2Collections - samples[i - 1].Gen2Collections));

        if (gen2Deltas.Count < 4)
            return false;

        var latestGen2 = gen2Deltas[^1];
        var gen2Median = Median(gen2Deltas.Select(d => (double)d).ToArray()[..^1]);
        return latestGen2 >= 2 && latestGen2 > gen2Median * 2d;
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0)
            return 0d;

        var ordered = values.OrderBy(v => v).ToArray();
        var mid = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[mid - 1] + ordered[mid]) / 2d
            : ordered[mid];
    }

    private static string Format(double value)
        => value.ToString("0.##");
}
