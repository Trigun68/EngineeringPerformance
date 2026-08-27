using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
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

    /// <summary>Пересчёт трудоёмкости PPV по сценарию планирования.</summary>
    IReadOnlyList<LaborIntensityCalculator.RecalcResult> RecalculateLaborIntensities(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection);

    /// <summary>Часы прогноза по id PPV (сценарий планирования).</summary>
    Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection);

    /// <summary>Агрегация прогнозных часов по направлениям ИИС (+ оборудование).</summary>
    Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IEnumerable<(int ClassifierId, decimal VolumeInIicUnits)> equipmentVolumes,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection);

    /// <summary>Часы прогноза по id строки оборудования (без группировки сценария ФО).</summary>
    Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeEquipmentForecastHours(
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection);

    /// <summary>Средневзвешенный Z по корневым строкам производительности.</summary>
    decimal? ComputeWeightedZFromPerformanceRows(
        IReadOnlyList<PerformanceRowDto> performanceRows,
        int decimalPlaces = 2);

    /// <summary>
    /// Средневзвешенный Z по произвольной выборке: Σ(Z×вес)/Σ(вес).
    /// Единственный разрешённый способ агрегировать Z вне корневых строк (ISS-010).
    /// </summary>
    decimal? ComputeWeightedZ(
        IEnumerable<(decimal? ZScore, decimal? Weight)> samples,
        int decimalPlaces = 2,
        MidpointRounding rounding = MidpointRounding.ToEven,
        bool arithmeticFallback = false);

    /// <summary>Z-параметры планирования по направлению ИИС (fallback — значения по умолчанию).</summary>
    PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId);

    /// <summary>
    /// Индекс производительности, % к норме: Σ(объём × норматив) / Σ(часы) × 100.
    /// Учитываются только строки, где одновременно известны объём, норма и часы
    /// (нулевые/отсутствующие часы исключают строку из обеих сумм — иначе объём без
    /// трудоёмкости завышал бы индекс). &gt;100 — быстрее нормы, &lt;100 — медленнее.
    /// null — нет ни одной полностью покрытой строки.
    /// </summary>
    decimal? ComputeProductivityIndex(IReadOnlyList<PerformanceRowDto> performanceRows);
}
