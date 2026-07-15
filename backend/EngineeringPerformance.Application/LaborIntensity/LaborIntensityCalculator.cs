using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.LaborIntensity;

/// <summary>Чистая логика пересчёта LI по сценариям планирования (без БД).</summary>
public static class LaborIntensityCalculator
{
    public sealed record PpvLine(
        int Id,
        int? ClassifierId,
        string? FacilityCode,
        decimal Quantity,
        decimal Coefficient,
        bool IsAssessment);

    public sealed record RecalcResult(int PpvId, decimal? Base, decimal? Pessimistic, decimal? Optimistic);

    public static IReadOnlyList<RecalcResult> Recalculate(
        PlanningScenario scenario,
        IReadOnlyList<PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        IDeviationCalculator calculator)
    {
        return scenario switch
        {
            PlanningScenario.ByEachValue => RecalculateByEachValue(lines, normsByDirection, planningZByDirection, calculator),
            PlanningScenario.ByProjectIicGroup => RecalculateByGroup(lines, normsByDirection, planningZByDirection, calculator, groupByFacility: false),
            PlanningScenario.ByObjectIicGroup => RecalculateByGroup(lines, normsByDirection, planningZByDirection, calculator, groupByFacility: true),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
    }

    private static IReadOnlyList<RecalcResult> RecalculateByEachValue(
        IReadOnlyList<PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> norms,
        IReadOnlyDictionary<int, PlanningZParameters> zParams,
        IDeviationCalculator calculator)
    {
        return lines.Where(l => l.IsAssessment && l.ClassifierId.HasValue).Select(line =>
        {
            var id = line.ClassifierId!.Value;
            if (!norms.TryGetValue(id, out var snap))
                return new RecalcResult(line.Id, null, null, null);

            var vol = line.Quantity * line.Coefficient;
            var z = ForecastCalculation.ResolvePlanningZ(zParams, id);
            var (b, o, p) = ForecastCalculation.ComputeForecastHours(snap.NormFormula, snap.DeviationFormula, vol, vol, z, calculator);
            return new RecalcResult(line.Id, b, p, o);
        }).ToList();
    }

    private static IReadOnlyList<RecalcResult> RecalculateByGroup(
        IReadOnlyList<PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> norms,
        IReadOnlyDictionary<int, PlanningZParameters> zParams,
        IDeviationCalculator calculator,
        bool groupByFacility)
    {
        var eligible = lines.Where(l => l.IsAssessment && l.ClassifierId.HasValue).ToList();
        var groups = eligible.GroupBy(l => (
            Dir: l.ClassifierId!.Value,
            Facility: groupByFacility ? l.FacilityCode ?? string.Empty : string.Empty));

        var result = new List<RecalcResult>();
        foreach (var group in groups)
        {
            if (!norms.TryGetValue(group.Key.Dir, out var snap))
                continue;

            var vGroup = group.Sum(l => l.Quantity * l.Coefficient);
            if (vGroup <= 0) continue;

            var z = ForecastCalculation.ResolvePlanningZ(zParams, group.Key.Dir);
            var (bG, oG, pG) = ForecastCalculation.ComputeForecastHours(snap.NormFormula, snap.DeviationFormula, vGroup, vGroup, z, calculator);

            foreach (var line in group)
            {
                var share = (line.Quantity * line.Coefficient) / vGroup;
                result.Add(new RecalcResult(
                    line.Id,
                    bG.HasValue ? Math.Round(bG.Value * share, 2) : null,
                    pG.HasValue ? Math.Round(pG.Value * share, 2) : null,
                    oG.HasValue ? Math.Round(oG.Value * share, 2) : null));
            }
        }

        return result;
    }
}
