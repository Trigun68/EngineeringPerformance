using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.LaborIntensity;

/// <summary>
/// Агрегация прогнозных часов по PPV и направлениям ИИС.
/// Реализация для IPerformanceCalculationEngine; Infrastructure вызывает только через движок.
/// </summary>
public static class ForecastHoursAggregator
{
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        IDeviationCalculator calculator)
    {
        if (lines.Count == 0)
            return new Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)>();

        var results = LaborIntensityCalculator.Recalculate(
            scenario,
            lines,
            normsByDirection,
            planningZByDirection,
            calculator);

        return results.ToDictionary(
            r => r.PpvId,
            r => (r.Base, r.Optimistic, r.Pessimistic));
    }

    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateByDirection(
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IEnumerable<(int ClassifierId, decimal VolumeInIicUnits)> equipmentVolumes,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        IDeviationCalculator calculator)
    {
        var ppvById = lines.ToDictionary(l => l.Id);
        var byDirection = new Dictionary<int, (decimal Base, decimal Optimistic, decimal Pessimistic)>();

        foreach (var (ppvId, hours) in ppvHoursById)
        {
            if (!ppvById.TryGetValue(ppvId, out var line) || line.ClassifierId is not int dirId)
                continue;

            if (!byDirection.TryGetValue(dirId, out var acc))
                acc = (0m, 0m, 0m);

            byDirection[dirId] = (
                acc.Base + (hours.Base ?? 0m),
                acc.Optimistic + (hours.Optimistic ?? 0m),
                acc.Pessimistic + (hours.Pessimistic ?? 0m));
        }

        foreach (var (classifierId, vol) in equipmentVolumes)
        {
            if (vol <= 0 || !normsByDirection.ContainsKey(classifierId))
                continue;

            var z = ForecastCalculation.ResolvePlanningZ(planningZByDirection, classifierId);
            var snap = normsByDirection[classifierId];
            var (b, o, p) = ForecastCalculation.ComputeForecastHours(
                snap.NormFormula,
                snap.DeviationFormula,
                vol,
                vol,
                z,
                calculator);

            if (!byDirection.TryGetValue(classifierId, out var acc))
                acc = (0m, 0m, 0m);

            byDirection[classifierId] = (
                acc.Base + (b ?? 0m),
                acc.Optimistic + (o ?? 0m),
                acc.Pessimistic + (p ?? 0m));
        }

        return byDirection.ToDictionary(
            kv => kv.Key,
            kv => ((decimal?)kv.Value.Base, (decimal?)kv.Value.Optimistic, (decimal?)kv.Value.Pessimistic));
    }
}
