using EngineeringPerformance.Application.Analytics;

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

    /// <summary>Сценарные часы (оптимистичный / пессимистичный) по Z-score нормы.</summary>
    (decimal Optimistic, decimal Pessimistic, string Range) ComputeForecastScenarioHours(
        decimal baseHours,
        decimal? performance,
        decimal? standardDeviation,
        decimal plannedVolume,
        decimal zOptimistic,
        decimal zPessimistic);
}
