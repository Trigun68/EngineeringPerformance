using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Analytics;

internal static class VersionCompareMerge
{
    public static List<VersionCompareFacilityRowDto> BuildFacilityRows(
        IReadOnlyList<FactFacilityRowInternal> facilityA,
        IReadOnlyList<FactFacilityRowInternal> facilityB)
    {
        var dictA = facilityA.GroupBy(f => f.Code).ToDictionary(g => g.Key, g => g.First());
        var dictB = facilityB.GroupBy(f => f.Code).ToDictionary(g => g.Key, g => g.First());
        var allCodes = dictA.Keys.Union(dictB.Keys).OrderBy(c => c);

        var result = new List<VersionCompareFacilityRowDto>();
        foreach (var code in allCodes)
        {
            dictA.TryGetValue(code, out var rowA);
            dictB.TryGetValue(code, out var rowB);

            var hoursA = rowA?.FactLaborHours;
            var hoursB = rowB?.FactLaborHours;
            var hoursDelta = hoursA.HasValue && hoursB.HasValue ? hoursB - hoursA : null;

            var zScoreA = rowA?.ZScore;
            var zScoreB = rowB?.ZScore;
            var zDelta = zScoreA.HasValue && zScoreB.HasValue ? zScoreB - zScoreA : null;

            result.Add(new VersionCompareFacilityRowDto(
                code,
                rowB?.Name ?? rowA?.Name ?? string.Empty,
                hoursA,
                hoursB,
                hoursDelta,
                zScoreA,
                zScoreB,
                zDelta,
                rowA?.Interpretation ?? string.Empty,
                rowB?.Interpretation ?? string.Empty));
        }

        return result;
    }

    public static List<VersionCompareDetailRowDto> BuildDetailRows(
        IReadOnlyList<FactDetailRowInternal> detailA,
        IReadOnlyList<FactDetailRowInternal> detailB)
    {
        static decimal? AverageProductivity(IEnumerable<FactDetailRowInternal> rows)
        {
            var values = rows.Where(d => d.FactProductivity.HasValue).Select(d => d.FactProductivity!.Value).ToList();
            return values.Count > 0 ? values.Average() : null;
        }

        var groupedA = detailA
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageProductivity(g)));

        var groupedB = detailB
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageProductivity(g)));

        var allKeys = groupedA.Keys.Union(groupedB.Keys)
            .OrderBy(k => k.FacilityCode)
            .ThenBy(k => k.DocumentSet)
            .ThenBy(k => k.PvCode)
            .ThenBy(k => k.WorkId);

        var result = new List<VersionCompareDetailRowDto>();
        foreach (var key in allKeys)
        {
            var hasA = groupedA.TryGetValue(key, out var rowA);
            var hasB = groupedB.TryGetValue(key, out var rowB);

            var quantityA = hasA ? (decimal?)rowA!.Quantity : null;
            var quantityB = hasB ? (decimal?)rowB!.Quantity : null;
            var quantityDelta = quantityA.HasValue && quantityB.HasValue ? quantityB - quantityA : null;

            var hoursA = hasA && rowA!.FactLaborHours > 0 ? (decimal?)Math.Round(rowA.FactLaborHours, 2) : null;
            var hoursB = hasB && rowB!.FactLaborHours > 0 ? (decimal?)Math.Round(rowB.FactLaborHours, 2) : null;
            var hoursDelta = hoursA.HasValue && hoursB.HasValue ? hoursB - hoursA : null;

            var prodA = hasA ? rowA!.FactProductivity : null;
            var prodB = hasB ? rowB!.FactProductivity : null;
            var prodDelta = prodA.HasValue && prodB.HasValue ? prodB - prodA : null;

            var presence = hasA && hasB ? "A+B" : hasA ? "только A" : "только B";
            var refRow = hasB ? rowB!.First : hasA ? rowA!.First : null;

            result.Add(new VersionCompareDetailRowDto(
                key.FacilityCode,
                refRow?.FacilityName ?? string.Empty,
                key.DocumentSet,
                key.PvCode,
                refRow?.PvName ?? string.Empty,
                key.WorkId,
                refRow?.WorkName ?? string.Empty,
                quantityA,
                quantityB,
                quantityDelta,
                hoursA,
                hoursB,
                hoursDelta,
                prodA,
                prodB,
                prodDelta,
                refRow?.IicGroupName ?? string.Empty,
                refRow?.IicUnitName ?? string.Empty,
                presence));
        }

        return result;
    }
}
