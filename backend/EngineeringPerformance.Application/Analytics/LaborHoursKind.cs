namespace EngineeringPerformance.Application.Analytics;

/// <summary>Какие часы LI подавать в PerformanceCalculationEngine.</summary>
public enum LaborHoursKind
{
    /// <summary>Плановые часы (оценка / план).</summary>
    Planned = 0,

    /// <summary>Фактические часы колонки ActualHours. На web-пути Z/KPI не используются.</summary>
    Actual = 1,
}
