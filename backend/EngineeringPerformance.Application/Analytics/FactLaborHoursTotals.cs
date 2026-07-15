using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Единая база часов факта для Assessment и Analysis PDF (FIX-02): сумма PlannedHours PLI.
/// </summary>
public static class FactLaborHoursTotals
{
    public const int WeightedZDecimalPlaces = 2;

    public static decimal? FromLaborIntensities(IEnumerable<ProjectLaborIntensity> laborIntensities)
    {
        var total = laborIntensities.Sum(li => li.PlannedHours);
        return total > 0 ? Math.Round(total, 2) : null;
    }

    public static decimal UnallocatedPlannedHours(IEnumerable<ProjectLaborIntensity> laborIntensities) =>
        laborIntensities.Where(li => !li.WorkDirectionId.HasValue).Sum(li => li.PlannedHours);
}
