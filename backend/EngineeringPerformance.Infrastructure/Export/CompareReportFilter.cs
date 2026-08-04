using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Фильтрация строк сравнения для PDF (паритет web compareFilters / docs).</summary>
public static class CompareReportFilter
{
    private const string PresenceBoth = "A+B";

    public static IReadOnlyList<VersionCompareRowDto> FilterSummary(
        IReadOnlyList<VersionCompareRowDto> rows,
        bool onlyDifferences)
    {
        if (!onlyDifferences) return rows;

        var byId = rows.Where(r => !r.IsTotal)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        var visible = new HashSet<int>();
        foreach (var row in rows)
        {
            if (row.IsTotal) continue;
            if (HasSummaryDifference(row))
                visible.Add(row.WorkDirectionId);
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var id in visible.ToList())
            {
                if (!byId.TryGetValue(id, out var row)) continue;
                if (row.ParentId is int parentId && visible.Add(parentId))
                    changed = true;
            }
        }

        return rows
            .Where(r =>
                visible.Contains(r.WorkDirectionId)
                || (r.IsTotal && r.Level == -1 && visible.Count > 0))
            .ToList();
    }

    public static IReadOnlyList<VersionCompareFacilityRowDto> FilterFacility(
        IReadOnlyList<VersionCompareFacilityRowDto> rows,
        bool onlyDifferences) =>
        onlyDifferences
            ? rows.Where(HasFacilityDifference).ToList()
            : rows;

    public static IReadOnlyList<VersionCompareDetailRowDto> FilterDetail(
        IReadOnlyList<VersionCompareDetailRowDto> rows,
        bool onlyDifferences)
    {
        if (!onlyDifferences) return rows;
        return rows.Where(HasDetailDifference).ToList();
    }

    /// <summary>FactZ + PlanZ + базовые плановые часы (PDF без выбора сценария).</summary>
    private static bool HasSummaryDifference(VersionCompareRowDto row)
    {
        var factZIdentical = Round4(row.FactZScoreA) == Round4(row.FactZScoreB);
        var planZIdentical = Round4(row.PlanZScoreA) == Round4(row.PlanZScoreB);
        var planLaborIdentical = Round2(row.PlannedLaborHoursA) == Round2(row.PlannedLaborHoursB);
        return !(factZIdentical && planZIdentical && planLaborIdentical);
    }

    private static bool HasFacilityDifference(VersionCompareFacilityRowDto row)
    {
        if (!row.FactLaborHoursA.HasValue && !row.FactLaborHoursB.HasValue)
            return false;

        var hoursIdentical = Round2(row.FactLaborHoursA) == Round2(row.FactLaborHoursB);
        var zIdentical = Round4(row.ZScoreA) == Round4(row.ZScoreB);
        return !(hoursIdentical && zIdentical);
    }

    private static bool HasDetailDifference(VersionCompareDetailRowDto row)
    {
        if (!string.Equals(row.Presence, PresenceBoth, StringComparison.Ordinal)
            && !string.Equals(row.Presence, "Both", StringComparison.Ordinal))
            return true;

        var quantityIdentical = Round4(row.QuantityA) == Round4(row.QuantityB);
        var hoursIdentical = Round2(row.FactLaborHoursA) == Round2(row.FactLaborHoursB);
        var prodIdentical = Round4(row.FactProductivityA) == Round4(row.FactProductivityB);
        return !(quantityIdentical && hoursIdentical && prodIdentical);
    }

    private static decimal Round4(decimal? value) =>
        Math.Round(value ?? 0m, 4, MidpointRounding.AwayFromZero);

    private static decimal Round2(decimal? value) =>
        Math.Round(value ?? 0m, 2, MidpointRounding.AwayFromZero);
}
