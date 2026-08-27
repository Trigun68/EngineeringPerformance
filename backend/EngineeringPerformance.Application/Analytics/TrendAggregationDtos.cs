namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Точка тренда до помесячной агрегации (Z или индекс). Вес — часы версии.
/// </summary>
public sealed record TrendPointSample(DateTime Date, decimal? Value, decimal Weight);

/// <summary>Помесячный бакет: взвешенное среднее точек версий.</summary>
public sealed record MonthlyTrendBucketDto(DateTime Date, decimal? Value);

/// <summary>KPI периода по помесячным бакетам: текущее / стартовое / изменение.</summary>
public sealed record PeriodKpiDto(decimal? Current, decimal? Initial, decimal? Delta);

/// <summary>Направление IIC с точками факта — вход водопада вкладов.</summary>
public sealed record TrendGroupSample(
    int GroupId,
    string GroupName,
    IReadOnlyList<TrendPointSample> Points);

/// <summary>Шаг водопада «Разбор изменения по факторам».</summary>
public sealed record WaterfallStepDto(string Name, decimal Value);

/// <summary>
/// Готовая агрегация детализированной тренд-вкладки: бакеты, KPI, водопад факта.
/// Считается только через IPerformanceCalculationEngine.
/// </summary>
public sealed record TrendDetailAggregationDto(
    IReadOnlyList<MonthlyTrendBucketDto> PlanMonthly,
    IReadOnlyList<MonthlyTrendBucketDto> FactMonthly,
    PeriodKpiDto PlanKpi,
    PeriodKpiDto FactKpi,
    IReadOnlyList<WaterfallStepDto>? FactWaterfall)
{
    public static TrendDetailAggregationDto Empty { get; } = new(
        [],
        [],
        new PeriodKpiDto(null, null, null),
        new PeriodKpiDto(null, null, null),
        null);
}
