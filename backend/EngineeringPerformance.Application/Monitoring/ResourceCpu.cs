namespace EngineeringPerformance.Application.Monitoring;

/// <summary>
/// CPU в долях логического ядра: 1.0 = одно ядро полностью за интервал.
/// </summary>
public static class ResourceCpu
{
    public static double CoresFromCpuSeconds(double previousCpuSeconds, double currentCpuSeconds, TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return 0d;

        var delta = currentCpuSeconds - previousCpuSeconds;
        if (delta < 0d)
            return 0d;

        return Round(delta / elapsed.TotalSeconds);
    }

    public static double CoresFromProcessorTime(TimeSpan previous, TimeSpan current, TimeSpan elapsed)
        => CoresFromCpuSeconds(previous.TotalSeconds, current.TotalSeconds, elapsed);

    /// <summary>
    /// SQL ProcessUtilization — процент от всех CPU хоста SQL; перевод в ядра через cpu_count.
    /// </summary>
    public static double CoresFromSqlProcessPercent(int processUtilizationPercent, int cpuCount)
    {
        if (cpuCount <= 0)
            return 0d;

        var clamped = Math.Clamp(processUtilizationPercent, 0, 100);
        return Round((clamped / 100d) * cpuCount);
    }

    public static double BytesToMib(long bytes)
        => Round(bytes / (1024d * 1024d));

    public static double Round(double value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
