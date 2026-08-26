using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Единая формула прогнозных часов: LI = N(V) · (1 + σ·z) · V_eff (мультипликативная модель).
/// Internal-деталь <see cref="Services.PerformanceCalculationEngine"/> — прямые вызовы
/// из Infrastructure запрещены, наружу торчит только IPerformanceCalculationEngine.
/// </summary>
internal static class ForecastCalculation
{
    public static PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId) =>
        workDirectionId >= 0
        && planningZByDirection.TryGetValue(workDirectionId, out var z)
            ? z
            : PlanningZParameters.Default;

    public static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        PlanningZParameters z,
        IDeviationCalculator calculator)
    {
        if (volume <= 0 || effectiveVolume <= 0)
            return (null, null, null);

        var baseH = Math.Round(calculator.CalculateLaborIntensity(normFormula, deviationFormula, volume, effectiveVolume, z.ZBase), 2);
        var optH = Math.Round(calculator.CalculateLaborIntensity(normFormula, deviationFormula, volume, effectiveVolume, z.ZOptimistic), 2);
        var pesH = Math.Round(calculator.CalculateLaborIntensity(normFormula, deviationFormula, volume, effectiveVolume, z.ZPessimistic), 2);
        return ClampNonNegativeHours((baseH, optH, pesH));
    }

    private static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ClampNonNegativeHours(
        (decimal? Base, decimal? Optimistic, decimal? Pessimistic) hours) =>
        (
            hours.Base.HasValue ? Math.Max(0m, hours.Base.Value) : null,
            hours.Optimistic.HasValue ? Math.Max(0m, hours.Optimistic.Value) : null,
            hours.Pessimistic.HasValue ? Math.Max(0m, hours.Pessimistic.Value) : null);

    /// <summary>Legacy scalar overload — строит Linear-формулу из Performance и σ-доли.</summary>
    public static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        decimal volume,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z)
    {
        if (volume <= 0 || !productivity.HasValue)
            return (null, null, null);

        var norm = NormFormulaMapper.FromLegacyPerformance(productivity);
        var dev = NormFormulaMapper.FromLegacyStdDev(productivity, stdDev);
        var calculator = new DeviationCalculator(new NormCalculator());
        var hours = ComputeForecastHours(norm, dev, volume, volume, z, calculator);
        return ClampNonNegativeHours(hours);
    }
}
