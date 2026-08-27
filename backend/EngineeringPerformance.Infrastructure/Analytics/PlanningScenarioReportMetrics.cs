using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Z плана для сводного отчёта = заложенный Z параметров планирования,
/// средневзвешенный по листьям IIC. Веса — базовые часы направления
/// (тот же rollup, что в диалоге «Параметры планирования»).
/// Часы сценариев — GrandTotals прогноза, без новой математики.
/// </summary>
internal static class PlanningScenarioReportMetrics
{
    public readonly record struct Result(
        decimal? ZPessimistic,
        decimal? ZBase,
        decimal? ZOptimistic,
        decimal? HoursPessimistic,
        decimal? HoursBase,
        decimal? HoursOptimistic);

    public static Result Compute(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine,
        ForecastScenarioTotalsDto grandTotals)
    {
        var (zPes, zBase, zOpt) = ComputePlanningWeightedZ(bundle, engine);
        return new Result(
            zPes,
            zBase,
            zOpt,
            PositiveOrNull(grandTotals.PessimisticHours),
            PositiveOrNull(grandTotals.BaseHours),
            PositiveOrNull(grandTotals.OptimisticHours));
    }

    /// <summary>
    /// Средневзвешенный Z по листьям: вес = Base-часы (как laborHoursWeight диалога).
    /// При нулевых весах — среднее арифметическое листьев.
    /// </summary>
    public static (decimal? Pessimistic, decimal? Base, decimal? Optimistic) ComputePlanningWeightedZ(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine)
    {
        var hoursByDir = ProjectsPageGridBuilder.ComputeLivePlanHoursByDirection(bundle);
        var parentIds = bundle.WorkDirections
            .Where(d => d.ParentId is int)
            .Select(d => d.ParentId!.Value)
            .ToHashSet();
        var leaves = bundle.WorkDirections.Where(d => !parentIds.Contains(d.Id));

        var pes = new List<(decimal? Z, decimal? W)>();
        var bas = new List<(decimal? Z, decimal? W)>();
        var opt = new List<(decimal? Z, decimal? W)>();

        foreach (var direction in leaves)
        {
            var z = engine.ResolvePlanningZ(bundle.PlanningZByDirection, direction.Id);
            hoursByDir.TryGetValue(direction.Id, out var triple);
            var weight = triple.Base;
            pes.Add((z.ZPessimistic, weight));
            bas.Add((z.ZBase, weight));
            opt.Add((z.ZOptimistic, weight));
        }

        const int places = FactLaborHoursTotals.WeightedZDecimalPlaces;
        return (
            engine.ComputeWeightedZ(pes, places, arithmeticFallback: true),
            engine.ComputeWeightedZ(bas, places, arithmeticFallback: true),
            engine.ComputeWeightedZ(opt, places, arithmeticFallback: true));
    }

    private static decimal? PositiveOrNull(decimal hours) => hours > 0 ? hours : null;
}
