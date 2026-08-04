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

        static decimal? PlannedProd(decimal? hours, decimal? volume) =>
            hours.HasValue && volume is > 0 ? hours.Value / volume.Value : null;

        static decimal? FactProd(decimal? hours, decimal? volume) =>
            hours.HasValue && volume is > 0 ? hours.Value / volume.Value : null;

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
                FactPhysicalVolumeA: rowA?.FactPhysicalVolume,
                FactPhysicalVolumeB: rowB?.FactPhysicalVolume,
                PlannedLaborHoursA: rowA?.PlannedLaborHoursBase,
                PlannedLaborHoursB: rowB?.PlannedLaborHoursBase,
                PlannedLaborHoursOptimisticA: rowA?.PlannedLaborHoursOptimistic,
                PlannedLaborHoursOptimisticB: rowB?.PlannedLaborHoursOptimistic,
                PlannedLaborHoursPessimisticA: rowA?.PlannedLaborHoursPessimistic,
                PlannedLaborHoursPessimisticB: rowB?.PlannedLaborHoursPessimistic,
                FactLaborHoursA: hoursA,
                FactLaborHoursB: hoursB,
                HoursDelta: hoursDelta,
                PlannedProductivityA: PlannedProd(rowA?.PlannedLaborHoursBase, rowA?.FactPhysicalVolume),
                PlannedProductivityB: PlannedProd(rowB?.PlannedLaborHoursBase, rowB?.FactPhysicalVolume),
                PlannedProductivityOptimisticA: PlannedProd(rowA?.PlannedLaborHoursOptimistic, rowA?.FactPhysicalVolume),
                PlannedProductivityOptimisticB: PlannedProd(rowB?.PlannedLaborHoursOptimistic, rowB?.FactPhysicalVolume),
                PlannedProductivityPessimisticA: PlannedProd(rowA?.PlannedLaborHoursPessimistic, rowA?.FactPhysicalVolume),
                PlannedProductivityPessimisticB: PlannedProd(rowB?.PlannedLaborHoursPessimistic, rowB?.FactPhysicalVolume),
                FactProductivityA: FactProd(hoursA, rowA?.FactPhysicalVolume),
                FactProductivityB: FactProd(hoursB, rowB?.FactPhysicalVolume),
                ZScoreA: zScoreA,
                ZScoreB: zScoreB,
                ZDelta: zDelta,
                InterpretationA: rowA?.Interpretation ?? string.Empty,
                InterpretationB: rowB?.Interpretation ?? string.Empty));
        }

        return result;
    }

    public static List<VersionCompareDetailRowDto> BuildDetailRows(
        IReadOnlyList<FactDetailRowInternal> detailA,
        IReadOnlyList<FactDetailRowInternal> detailB)
    {
        static decimal? AverageNullable(IEnumerable<decimal?> values)
        {
            var list = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return list.Count > 0 ? list.Average() : null;
        }

        static decimal? SumNullable(IEnumerable<decimal?> values)
        {
            var list = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return list.Count > 0 ? Math.Round(list.Sum(), 2) : null;
        }

        static decimal? PlannedProd(decimal? hours, decimal? quantity) =>
            hours.HasValue && quantity is > 0 ? hours.Value / quantity.Value : null;

        var groupedA = detailA
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    PlanBase: SumNullable(g.Select(d => d.PlannedLaborHoursBase)),
                    PlanOpt: SumNullable(g.Select(d => d.PlannedLaborHoursOptimistic)),
                    PlanPes: SumNullable(g.Select(d => d.PlannedLaborHoursPessimistic)),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageNullable(g.Select(d => d.FactProductivity)),
                    ZScore: AverageNullable(g.Select(d => d.ZScore))));

        var groupedB = detailB
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    PlanBase: SumNullable(g.Select(d => d.PlannedLaborHoursBase)),
                    PlanOpt: SumNullable(g.Select(d => d.PlannedLaborHoursOptimistic)),
                    PlanPes: SumNullable(g.Select(d => d.PlannedLaborHoursPessimistic)),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageNullable(g.Select(d => d.FactProductivity)),
                    ZScore: AverageNullable(g.Select(d => d.ZScore))));

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

            var zA = hasA ? rowA!.ZScore : null;
            var zB = hasB ? rowB!.ZScore : null;
            var zDelta = zA.HasValue && zB.HasValue ? zB - zA : null;

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
                QuantityA: quantityA,
                QuantityB: quantityB,
                QuantityDelta: quantityDelta,
                PlannedLaborHoursA: hasA ? rowA!.PlanBase : null,
                PlannedLaborHoursB: hasB ? rowB!.PlanBase : null,
                PlannedLaborHoursOptimisticA: hasA ? rowA!.PlanOpt : null,
                PlannedLaborHoursOptimisticB: hasB ? rowB!.PlanOpt : null,
                PlannedLaborHoursPessimisticA: hasA ? rowA!.PlanPes : null,
                PlannedLaborHoursPessimisticB: hasB ? rowB!.PlanPes : null,
                FactLaborHoursA: hoursA,
                FactLaborHoursB: hoursB,
                HoursDelta: hoursDelta,
                PlannedProductivityA: hasA ? PlannedProd(rowA!.PlanBase, quantityA) : null,
                PlannedProductivityB: hasB ? PlannedProd(rowB!.PlanBase, quantityB) : null,
                PlannedProductivityOptimisticA: hasA ? PlannedProd(rowA!.PlanOpt, quantityA) : null,
                PlannedProductivityOptimisticB: hasB ? PlannedProd(rowB!.PlanOpt, quantityB) : null,
                PlannedProductivityPessimisticA: hasA ? PlannedProd(rowA!.PlanPes, quantityA) : null,
                PlannedProductivityPessimisticB: hasB ? PlannedProd(rowB!.PlanPes, quantityB) : null,
                FactProductivityA: prodA,
                FactProductivityB: prodB,
                ProductivityDelta: prodDelta,
                ZScoreA: zA,
                ZScoreB: zB,
                ZDelta: zDelta,
                IicGroupName: refRow?.IicGroupName ?? string.Empty,
                IicUnitName: refRow?.IicUnitName ?? string.Empty,
                Presence: presence,
                BindingType: refRow?.BindingType ?? string.Empty,
                CatalogId: refRow?.CatalogId ?? string.Empty,
                Tag: refRow?.Tag ?? string.Empty,
                ItemName: refRow?.ItemName ?? string.Empty,
                WorkComment: refRow?.WorkComment ?? string.Empty,
                EstimationBasis: refRow?.EstimationBasis ?? string.Empty,
                IsAssessment: refRow?.IsAssessment ?? string.Empty,
                Coefficient: refRow?.Coefficient,
                NormProductivity: refRow?.NormProductivity,
                NormStdDev: refRow?.NormStdDev,
                IsEquipment: refRow?.IsEquipment ?? false));
        }

        return result;
    }
}
