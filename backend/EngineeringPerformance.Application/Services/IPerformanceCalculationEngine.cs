using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Единый движок расчётов производительности и распределения LI (Single Source of Truth).
/// Не зависит от DbContext — принимает только DTO.
/// </summary>
public interface IPerformanceCalculationEngine
{
    IReadOnlyList<PerformanceRowDto> CalculatePerformance(PerformanceCalculationInput input);

    FactAssessmentCalculationResult CalculateFactAssessment(FactAssessmentCalculationInput input);

    /// <summary>Базовые часы прогноза по методу element/regression или LI.</summary>
    decimal ComputeForecastBaseHours(
        decimal plannedVolume,
        decimal? performance,
        decimal? estimate,
        decimal laborHours,
        string method);

    /// <summary>Прогнозные часы по параметрической норме и Z-параметрам планирования.</summary>
    (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        PlanningZParameters z);

    /// <summary>Legacy scalar overload.</summary>
    (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        decimal volume,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z);
}
