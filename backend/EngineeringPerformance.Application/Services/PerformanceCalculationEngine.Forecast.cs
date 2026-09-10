using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Services;

public sealed partial class PerformanceCalculationEngine
{
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

    public LaborIntensityCalculator.PlanHoursResult RecalculatePlanHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> ppvLines,
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> equipmentLines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        LaborIntensityCalculator.Recalculate(
            scenario, ppvLines, equipmentLines, normsByDirection, planningZByDirection, _deviationCalculator);

    public PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId) =>
        ForecastCalculation.ResolvePlanningZ(planningZByDirection, workDirectionId);
}
