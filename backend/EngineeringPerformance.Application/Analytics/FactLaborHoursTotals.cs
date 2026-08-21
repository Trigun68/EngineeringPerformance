using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Суммы часов LI: план = PlannedHours, факт ввода = ActualHours.
/// </summary>
public static class FactLaborHoursTotals
{
    public const int WeightedZDecimalPlaces = 2;

    public static decimal? FromLaborIntensities(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        FromHours(laborIntensities.Sum(li => li.PlannedHours));

    public static decimal? FromActualHours(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        FromHours(laborIntensities.Sum(li => li.ActualHours));

    /// <summary>Σ ActualHours только по строкам с группой IIC. Неотнесённые не входят в KPI факта.</summary>
    public static decimal? FromAllocatedActualHours(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        FromHours(laborIntensities.Where(li => li.WorkDirectionId.HasValue).Sum(li => li.ActualHours));

    public static decimal UnallocatedPlannedHours(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        laborIntensities.Where(li => !li.WorkDirectionId.HasValue).Sum(li => li.PlannedHours);

    public static decimal UnallocatedActualHours(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        laborIntensities.Where(li => !li.WorkDirectionId.HasValue).Sum(li => li.ActualHours);

    private static decimal? FromHours(decimal total) =>
        total > 0 ? Math.Round(total, 2) : null;
}
