namespace EngineeringPerformance.Core.Services;

/// <summary>Z-параметры сценариев прогноза по группе IIC (пессимист./базовый/оптимист.).</summary>
public sealed record PlanningZParameters(decimal ZPessimistic, decimal ZBase, decimal ZOptimistic)
{
    public static PlanningZParameters Default { get; } = new(0.5m, 0m, -0.5m);
}

/// <summary>
/// Единая формула прогнозных часов для всех трёх сценариев.
/// Каждый сценарий: Объём × (Performance + Z_сценария × StdDev), Z берётся из параметров планирования.
/// </summary>
public static class ForecastCalculation
{
    public static PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId) =>
        workDirectionId >= 0
        && planningZByDirection.TryGetValue(workDirectionId, out var z)
            ? z
            : PlanningZParameters.Default;

    public static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        decimal volume,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z)
    {
        if (volume <= 0 || !productivity.HasValue)
            return (null, null, null);

        var p = productivity.Value;
        var std = stdDev ?? 0m;
        var baseH = Math.Round(volume * (p + z.ZBase * std), 2);
        if (std <= 0)
            return (baseH, baseH, baseH);

        var optH = Math.Round(volume * Math.Max(0m, p + z.ZOptimistic * std), 2);
        var pesH = Math.Round(volume * (p + z.ZPessimistic * std), 2);
        return (baseH, optH, pesH);
    }
}
