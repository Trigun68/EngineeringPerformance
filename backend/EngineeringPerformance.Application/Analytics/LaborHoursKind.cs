namespace EngineeringPerformance.Application.Analytics;

/// <summary>Какие часы LI подавать в PerformanceCalculationEngine.</summary>
public enum LaborHoursKind
{
    /// <summary>Плановые часы (оценка / план).</summary>
    Planned = 0,

    /// <summary>Фактические часы (ActualHours).</summary>
    Actual = 1,
}
