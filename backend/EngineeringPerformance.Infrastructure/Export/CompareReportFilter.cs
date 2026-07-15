using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Фильтрация строк сравнения для PDF (паритет web compareFilters).</summary>
internal static class CompareReportFilter
{
    private const string PresenceBoth = "A+B";

    public static IReadOnlyList<VersionCompareRowDto> FilterSummary(
        IReadOnlyList<VersionCompareRowDto> rows,
        bool onlyDifferences)
    {
        if (!onlyDifferences) return rows;

        var diffIds = rows
            .Where(r => r.ZDelta is not null && r.ZDelta != 0)
            .Select(r => r.WorkDirectionId)
            .ToHashSet();

        var keep = new HashSet<int>();
        foreach (var id in diffIds)
            AddAncestors(rows, id, keep);

        return rows.Where(r => keep.Contains(r.WorkDirectionId)).ToList();
    }

    public static IReadOnlyList<VersionCompareFacilityRowDto> FilterFacility(
        IReadOnlyList<VersionCompareFacilityRowDto> rows,
        bool onlyDifferences) =>
        onlyDifferences
            ? rows.Where(r => (r.HoursDelta ?? 0) != 0 || (r.ZDelta ?? 0) != 0).ToList()
            : rows;

    public static IReadOnlyList<VersionCompareDetailRowDto> FilterDetail(
        IReadOnlyList<VersionCompareDetailRowDto> rows,
        bool onlyDifferences)
    {
        if (!onlyDifferences) return rows;
        return rows.Where(HasDetailDifference).ToList();
    }

    private static bool HasDetailDifference(VersionCompareDetailRowDto row)
    {
        if (!string.Equals(row.Presence, PresenceBoth, StringComparison.Ordinal))
            return true;
        return (row.QuantityDelta ?? 0) != 0
            || (row.HoursDelta ?? 0) != 0
            || (row.ProductivityDelta ?? 0) != 0;
    }

    private static void AddAncestors(IReadOnlyList<VersionCompareRowDto> rows, int id, HashSet<int> keep)
    {
        var byId = rows.ToDictionary(r => r.WorkDirectionId);
        var current = id;
        while (byId.TryGetValue(current, out var row))
        {
            keep.Add(current);
            if (row.ParentId is null) break;
            current = row.ParentId.Value;
        }
    }
}
