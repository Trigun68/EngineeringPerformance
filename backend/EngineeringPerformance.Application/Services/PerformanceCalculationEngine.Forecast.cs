using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Services;

public sealed partial class PerformanceCalculationEngine
{
    public decimal ComputeForecastBaseHours(
        decimal plannedVolume,
        decimal? performance,
        decimal? estimate,
        decimal laborHours,
        string method)
    {
        if (plannedVolume > 0)
        {
            if (string.Equals(method, "element", StringComparison.OrdinalIgnoreCase)
                && estimate is { } est && est > 0)
            {
                return Math.Round(plannedVolume * est, 2);
            }

            if (performance is { } p && p > 0)
            {
                return Math.Round(plannedVolume * p, 2);
            }
        }

        return laborHours;
    }

    public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        PlanningZParameters z) =>
        ForecastCalculation.ComputeForecastHours(normFormula, deviationFormula, volume, effectiveVolume, z, _deviationCalculator);

    public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        decimal volume,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z) =>
        ForecastCalculation.ComputeForecastHours(volume, productivity, stdDev, z, _deviationCalculator);

    public IReadOnlyList<LaborIntensityCalculator.RecalcResult> RecalculateLaborIntensities(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        RecalculatePlanHours(
            scenario, lines, Array.Empty<LaborIntensityCalculator.EquipmentLine>(),
            normsByDirection, planningZByDirection).Ppv;

    public LaborIntensityCalculator.PlanHoursResult RecalculatePlanHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> ppvLines,
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> equipmentLines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        LaborIntensityCalculator.Recalculate(
            scenario, ppvLines, equipmentLines, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        ForecastHoursAggregator.ComputePpvHours(
            scenario, lines, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IEnumerable<(int ClassifierId, decimal VolumeInIicUnits)> equipmentVolumes,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        ForecastHoursAggregator.AggregateByDirection(
            lines, ppvHoursById, equipmentVolumes, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeEquipmentForecastHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> lines,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> ppvLines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        RecalculatePlanHours(scenario, ppvLines, lines, normsByDirection, planningZByDirection).Equipment;

    public PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId) =>
        ForecastCalculation.ResolvePlanningZ(planningZByDirection, workDirectionId);
}
