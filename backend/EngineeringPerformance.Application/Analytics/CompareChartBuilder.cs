namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Диаграмма сравнения из уже посчитанных summary/facility — без повторного CompareAsync.
/// </summary>
public static class CompareChartBuilder
{
    public static CompareChartResultDto FromCompare(
        VersionCompareResultDto compare,
        string versionAName,
        string versionBName,
        string level = "iic",
        string metric = "zScore",
        int top = 20)
    {
        var take = Math.Clamp(top, 1, 100);
        var byHours = metric.Equals("hours", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(level, "facility", StringComparison.OrdinalIgnoreCase))
        {
            var points = compare.Facility
                .Select(f => new CompareChartPointDto(
                    f.FacilityName,
                    null,
                    byHours ? f.FactLaborHoursA : f.ZScoreA,
                    byHours ? f.FactLaborHoursB : f.ZScoreB,
                    byHours ? f.HoursDelta : f.ZDelta))
                .OrderByDescending(p => Math.Abs(p.Delta ?? 0m))
                .Take(take)
                .ToList();

            return new CompareChartResultDto("facility", metric, versionAName, versionBName, points);
        }

        var iicPoints = compare.Summary
            .Where(r => r.WorkDirectionId >= 0 && !r.IsTotal)
            .Select(r => new CompareChartPointDto(
                r.Name,
                r.WorkDirectionId,
                byHours ? r.PlannedLaborHoursA : r.FactZScoreA,
                byHours ? r.PlannedLaborHoursB : r.FactZScoreB,
                byHours
                    ? (r.PlannedLaborHoursA.HasValue && r.PlannedLaborHoursB.HasValue
                        ? r.PlannedLaborHoursB - r.PlannedLaborHoursA
                        : null)
                    : r.FactZDelta))
            .OrderByDescending(p => Math.Abs(p.Delta ?? 0m))
            .Take(take)
            .ToList();

        return new CompareChartResultDto("iic", metric, versionAName, versionBName, iicPoints);
    }
}
