using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

public static partial class ProjectsPageGridBuilder
{
    /// <summary>
    /// Сводка сравнения по направлениям: факт (ФО, труд, произв., Z) + план (Base/Opt/Pess).
    /// </summary>
    public static IReadOnlyList<VersionCompareRowDto> BuildCompareSummary(
        IReadOnlyList<PerformanceRowDto> performanceA,
        IReadOnlyList<PerformanceRowDto> performanceB,
        ProjectsPageVersionBundle bundleA,
        ProjectsPageVersionBundle bundleB,
        IPerformanceCalculationEngine engine)
    {
        var dictA = performanceA.Where(r => !r.IsTotal)
            .GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var dictB = performanceB.Where(r => !r.IsTotal)
            .GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());

        var hoursA = ResolveScenarioHoursByDirection(bundleA, performanceA);
        var hoursB = ResolveScenarioHoursByDirection(bundleB, performanceB);
        var planZBaseA = PlanZByDirection(bundleA, engine, PlanHourKind.Base);
        var planZOptA = PlanZByDirection(bundleA, engine, PlanHourKind.Optimistic);
        var planZPesA = PlanZByDirection(bundleA, engine, PlanHourKind.Pessimistic);
        var planZBaseB = PlanZByDirection(bundleB, engine, PlanHourKind.Base);
        var planZOptB = PlanZByDirection(bundleB, engine, PlanHourKind.Optimistic);
        var planZPesB = PlanZByDirection(bundleB, engine, PlanHourKind.Pessimistic);

        var allIds = dictA.Keys.Union(dictB.Keys).ToList();
        var summaryBase = new List<VersionCompareRowDto>(allIds.Count);
        foreach (var id in allIds)
        {
            dictA.TryGetValue(id, out var a);
            dictB.TryGetValue(id, out var b);
            if (a is null && b is null) continue;

            hoursA.TryGetValue(id, out var plannedHoursA);
            hoursB.TryGetValue(id, out var plannedHoursB);
            summaryBase.Add(MapCompareSummaryRow(
                a,
                b,
                plannedHoursA,
                plannedHoursB,
                planZBaseA.GetValueOrDefault(id),
                planZBaseB.GetValueOrDefault(id),
                planZOptA.GetValueOrDefault(id),
                planZOptB.GetValueOrDefault(id),
                planZPesA.GetValueOrDefault(id),
                planZPesB.GetValueOrDefault(id)));
        }

        return AppendCompareTotals(summaryBase, dictA, dictB, bundleA, bundleB, engine);
    }

    public static ProjectsPageCompareGridsDto BuildCompareGrids(
        IReadOnlyList<PerformanceRowDto> performanceA,
        IReadOnlyList<PerformanceRowDto> performanceB,
        ProjectsPageFactGridsDto factA,
        ProjectsPageFactGridsDto factB,
        ProjectsPageVersionBundle bundleA,
        ProjectsPageVersionBundle bundleB,
        IPerformanceCalculationEngine engine)
    {
        var summary = BuildCompareSummary(performanceA, performanceB, bundleA, bundleB, engine);

        var detailA = factA.Detail.Select(ToCompareDetailInternal).ToList();
        var detailB = factB.Detail.Select(ToCompareDetailInternal).ToList();

        var facilityA = EnrichFacilityFromDetail(
            factA.Facility.Select(f => new FactFacilityRowInternal
            {
                Code = f.FacilityCode,
                Name = f.FacilityName,
                FactLaborHours = f.LaborHours,
                ZScore = f.ZScore,
                Interpretation = f.Interpretation,
                PlannedLaborHoursBase = f.BaseHours,
                PlannedLaborHoursOptimistic = f.OptimisticHours,
                PlannedLaborHoursPessimistic = f.PessimisticHours,
            }).ToList(),
            detailA);
        var facilityB = EnrichFacilityFromDetail(
            factB.Facility.Select(f => new FactFacilityRowInternal
            {
                Code = f.FacilityCode,
                Name = f.FacilityName,
                FactLaborHours = f.LaborHours,
                ZScore = f.ZScore,
                Interpretation = f.Interpretation,
                PlannedLaborHoursBase = f.BaseHours,
                PlannedLaborHoursOptimistic = f.OptimisticHours,
                PlannedLaborHoursPessimistic = f.PessimisticHours,
            }).ToList(),
            detailB);

        return new ProjectsPageCompareGridsDto(
            summary,
            VersionCompareMerge.BuildFacilityRows(facilityA, facilityB),
            VersionCompareMerge.BuildDetailRows(detailA, detailB, engine));
    }

    /// <summary>
    /// Добавляет корневые «Итого: …» и «ИТОГО» в summary сравнения с ФО по ЕИ (A и B отдельно).
    /// План на «Итого:» — own+потомки; «ИТОГО» плана — SumAllForecastScenarioHours (как прогноз).
    /// Факт на итогах — сумма own FactLaborHours (как AppendPerformanceTotals).
    /// </summary>
    public static IReadOnlyList<VersionCompareRowDto> AppendCompareTotals(
        IReadOnlyList<VersionCompareRowDto> rows,
        IReadOnlyDictionary<int, PerformanceRowDto> performanceA,
        IReadOnlyDictionary<int, PerformanceRowDto> performanceB,
        ProjectsPageVersionBundle bundleA,
        ProjectsPageVersionBundle bundleB,
        IPerformanceCalculationEngine engine)
    {
        var source = rows.Where(r => !r.IsTotal).ToList();
        var byId = source.GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var childrenByParent = source
            .Where(r => r.ParentId.HasValue && r.WorkDirectionId >= 0)
            .ToLookup(r => r.ParentId!.Value);
        var roots = source.Where(r => r.WorkDirectionId >= 0 && r.ParentId is null).ToList();

        var childrenPerfA = performanceA.Values
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId.HasValue)
            .ToLookup(r => r.ParentId!.Value);
        var childrenPerfB = performanceB.Values
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId.HasValue)
            .ToLookup(r => r.ParentId!.Value);

        var result = new List<VersionCompareRowDto>();

        CompareHoursRollup BuildSubtree(VersionCompareRowDto row)
        {
            result.Add(row);
            var children = childrenByParent[row.WorkDirectionId]
                .Where(c => byId.ContainsKey(c.WorkDirectionId))
                .ToList();
            var own = OwnCompareHours(row);
            if (children.Count == 0)
                return own;

            var acc = own;
            foreach (var child in children)
                acc = AddCompareHours(acc, BuildSubtree(child));

            performanceA.TryGetValue(row.WorkDirectionId, out var perfA);
            performanceB.TryGetValue(row.WorkDirectionId, out var perfB);
            var foA = perfA is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatFromPerformance(CollectSubtree(perfA, childrenPerfA));
            var foB = perfB is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatFromPerformance(CollectSubtree(perfB, childrenPerfB));
            var physicalA = perfA is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(CollectSubtree(perfA, childrenPerfA));
            var physicalB = perfB is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(CollectSubtree(perfB, childrenPerfB));
            var assessmentA = perfA is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(CollectSubtree(perfA, childrenPerfA));
            var assessmentB = perfB is null
                ? null
                : PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(CollectSubtree(perfB, childrenPerfB));

            result.Add(new VersionCompareRowDto(
                row.WorkDirectionId,
                row.ParentId,
                $"Итого: {row.Name}",
                FactPhysicalVolumeDisplayA: foA,
                FactPhysicalVolumeDisplayB: foB,
                PhysicalVolumeDisplayA: physicalA,
                PhysicalVolumeDisplayB: physicalB,
                AssessmentPhysicalVolumeDisplayA: assessmentA,
                AssessmentPhysicalVolumeDisplayB: assessmentB,
                PlannedLaborHoursA: NullIfNonPositive(acc.BaseA),
                PlannedLaborHoursB: NullIfNonPositive(acc.BaseB),
                PlannedLaborHoursOptimisticA: NullIfNonPositive(acc.OptA),
                PlannedLaborHoursOptimisticB: NullIfNonPositive(acc.OptB),
                PlannedLaborHoursPessimisticA: NullIfNonPositive(acc.PesA),
                PlannedLaborHoursPessimisticB: NullIfNonPositive(acc.PesB),
                FactLaborHoursA: NullIfNonPositive(acc.FactA),
                FactLaborHoursB: NullIfNonPositive(acc.FactB),
                IsTotal: true,
                Level: row.Level));
            return acc;
        }

        foreach (var root in roots)
            BuildSubtree(root);

        foreach (var unallocated in source.Where(r => r.WorkDirectionId < 0))
            result.Add(unallocated);

        var rootTotals = result.Where(r => r.IsTotal && r.Level == 0).ToList();
        var hasFo = performanceA.Values.Concat(performanceB.Values)
            .Any(r => !r.IsTotal && r.WorkDirectionId >= 0
                && (r.FactPhysicalVolume is > 0 || r.PhysicalVolume is > 0 || r.AssessmentPhysicalVolume is > 0));
        if (rootTotals.Count > 0 || hasFo || source.Count > 0)
        {
            var grandFoA = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
                performanceA.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandFoB = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
                performanceB.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandPhysicalA = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                performanceA.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandPhysicalB = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                performanceB.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandAssessmentA = PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(
                performanceA.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandAssessmentB = PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(
                performanceB.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var allocated = source.Where(r => r.WorkDirectionId >= 0).ToList();
            var grandFactA = allocated.Sum(r => r.FactLaborHoursA ?? 0m);
            var grandFactB = allocated.Sum(r => r.FactLaborHoursB ?? 0m);
            var grandPlanA = SumAllForecastScenarioHours(bundleA);
            var grandPlanB = SumAllForecastScenarioHours(bundleB);
            result.Add(new VersionCompareRowDto(
                int.MinValue,
                null,
                "ИТОГО",
                FactPhysicalVolumeDisplayA: grandFoA,
                FactPhysicalVolumeDisplayB: grandFoB,
                PhysicalVolumeDisplayA: grandPhysicalA,
                PhysicalVolumeDisplayB: grandPhysicalB,
                AssessmentPhysicalVolumeDisplayA: grandAssessmentA,
                AssessmentPhysicalVolumeDisplayB: grandAssessmentB,
                PlannedLaborHoursA: NullIfNonPositive(grandPlanA.Base),
                PlannedLaborHoursB: NullIfNonPositive(grandPlanB.Base),
                PlannedLaborHoursOptimisticA: NullIfNonPositive(grandPlanA.Opt),
                PlannedLaborHoursOptimisticB: NullIfNonPositive(grandPlanB.Opt),
                PlannedLaborHoursPessimisticA: NullIfNonPositive(grandPlanA.Pes),
                PlannedLaborHoursPessimisticB: NullIfNonPositive(grandPlanB.Pes),
                FactLaborHoursA: NullIfNonPositive(grandFactA),
                FactLaborHoursB: NullIfNonPositive(grandFactB),
                IsTotal: true,
                Level: -1));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    private readonly record struct CompareHoursRollup(
        decimal FactA, decimal FactB,
        decimal PesA, decimal BaseA, decimal OptA,
        decimal PesB, decimal BaseB, decimal OptB);

    private static CompareHoursRollup OwnCompareHours(VersionCompareRowDto row) => new(
        row.FactLaborHoursA ?? 0m,
        row.FactLaborHoursB ?? 0m,
        row.PlannedLaborHoursPessimisticA ?? 0m,
        row.PlannedLaborHoursA ?? 0m,
        row.PlannedLaborHoursOptimisticA ?? 0m,
        row.PlannedLaborHoursPessimisticB ?? 0m,
        row.PlannedLaborHoursB ?? 0m,
        row.PlannedLaborHoursOptimisticB ?? 0m);

    private static CompareHoursRollup AddCompareHours(CompareHoursRollup a, CompareHoursRollup b) => new(
        a.FactA + b.FactA,
        a.FactB + b.FactB,
        a.PesA + b.PesA,
        a.BaseA + b.BaseA,
        a.OptA + b.OptA,
        a.PesB + b.PesB,
        a.BaseB + b.BaseB,
        a.OptB + b.OptB);

    private static Dictionary<int, decimal?> PlanZByDirection(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine,
        PlanHourKind kind) =>
        BuildPlanPerformance(bundle, engine, kind)
            .Where(r => !r.IsTotal)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First().ZScore);

    private static VersionCompareRowDto MapCompareSummaryRow(
        PerformanceRowDto? a,
        PerformanceRowDto? b,
        (decimal? Base, decimal? Optimistic, decimal? Pessimistic) plannedHoursA,
        (decimal? Base, decimal? Optimistic, decimal? Pessimistic) plannedHoursB,
        decimal? planZA,
        decimal? planZB,
        decimal? planZOptA,
        decimal? planZOptB,
        decimal? planZPesA,
        decimal? planZPesB)
    {
        var row = a ?? b!;
        var factZA = a?.ZScore;
        var factZB = b?.ZScore;
        decimal? factZDelta = factZA.HasValue && factZB.HasValue ? factZB - factZA : null;
        decimal? planZAValue = a is null ? null : planZA;
        decimal? planZBValue = b is null ? null : planZB;
        decimal? planZDelta = planZAValue.HasValue && planZBValue.HasValue
            ? planZBValue - planZAValue
            : null;
        decimal? planZOptAValue = a is null ? null : planZOptA;
        decimal? planZOptBValue = b is null ? null : planZOptB;
        decimal? planZPesAValue = a is null ? null : planZPesA;
        decimal? planZPesBValue = b is null ? null : planZPesB;

        static decimal? PlannedProductivity(decimal? hours, decimal? volume) =>
            hours.HasValue && volume is > 0 ? hours.Value / volume.Value : null;

        var hoursA = a is null ? (null, null, null) : plannedHoursA;
        var hoursB = b is null ? (null, null, null) : plannedHoursB;

        return new VersionCompareRowDto(
            row.WorkDirectionId,
            row.ParentId,
            row.WorkDirectionName,
            FactZScoreA: factZA,
            FactZScoreB: factZB,
            FactZDelta: factZDelta,
            InterpretationA: a?.Interpretation.ToRussian() is { Length: > 0 } ia ? ia : "-",
            InterpretationB: b?.Interpretation.ToRussian() is { Length: > 0 } ib ? ib : "-",
            FactPhysicalVolumeA: a?.FactPhysicalVolume,
            FactPhysicalVolumeB: b?.FactPhysicalVolume,
            PhysicalVolumeA: a?.PhysicalVolume,
            PhysicalVolumeB: b?.PhysicalVolume,
            AssessmentPhysicalVolumeA: a?.AssessmentPhysicalVolume,
            AssessmentPhysicalVolumeB: b?.AssessmentPhysicalVolume,
            PlannedLaborHoursA: hoursA.Base,
            PlannedLaborHoursB: hoursB.Base,
            PlannedLaborHoursOptimisticA: hoursA.Optimistic,
            PlannedLaborHoursOptimisticB: hoursB.Optimistic,
            PlannedLaborHoursPessimisticA: hoursA.Pessimistic,
            PlannedLaborHoursPessimisticB: hoursB.Pessimistic,
            PlannedProductivityA: PlannedProductivity(hoursA.Base, a?.PhysicalVolume),
            PlannedProductivityB: PlannedProductivity(hoursB.Base, b?.PhysicalVolume),
            PlannedProductivityOptimisticA: PlannedProductivity(hoursA.Optimistic, a?.PhysicalVolume),
            PlannedProductivityOptimisticB: PlannedProductivity(hoursB.Optimistic, b?.PhysicalVolume),
            PlannedProductivityPessimisticA: PlannedProductivity(hoursA.Pessimistic, a?.PhysicalVolume),
            PlannedProductivityPessimisticB: PlannedProductivity(hoursB.Pessimistic, b?.PhysicalVolume),
            FactLaborHoursA: a?.FactLaborHours,
            FactLaborHoursB: b?.FactLaborHours,
            FactProductivityA: a?.FactProductivity,
            FactProductivityB: b?.FactProductivity,
            PlanZScoreA: planZAValue,
            PlanZScoreB: planZBValue,
            PlanZDelta: planZDelta,
            PlanZScoreOptimisticA: planZOptAValue,
            PlanZScoreOptimisticB: planZOptBValue,
            PlanZScorePessimisticA: planZPesAValue,
            PlanZScorePessimisticB: planZPesBValue,
            IsTotal: false,
            Level: row.Level);
    }

    private static List<FactFacilityRowInternal> EnrichFacilityFromDetail(
        IReadOnlyList<FactFacilityRowInternal> facility,
        IReadOnlyList<FactDetailRowInternal> detail)
    {
        var byCode = detail
            .GroupBy(d => d.FacilityCode)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var hoursVol = g.Where(d => d.FactLaborHours is > 0).Sum(d => d.Quantity);
                    var allVol = g.Sum(d => d.Quantity);
                    return (
                        Name: g.First().FacilityName,
                        Volume: hoursVol > 0 ? hoursVol : allVol);
                });

        var result = facility
            .Select(f =>
            {
                byCode.TryGetValue(f.Code, out var agg);
                return new FactFacilityRowInternal
                {
                    Code = f.Code,
                    Name = f.Name,
                    FactLaborHours = f.FactLaborHours,
                    ZScore = f.ZScore,
                    Interpretation = f.Interpretation,
                    FactPhysicalVolume = agg.Volume > 0 ? agg.Volume : f.FactPhysicalVolume,
                    PlannedLaborHoursBase = f.PlannedLaborHoursBase,
                    PlannedLaborHoursOptimistic = f.PlannedLaborHoursOptimistic,
                    PlannedLaborHoursPessimistic = f.PlannedLaborHoursPessimistic,
                };
            })
            .ToList();

        var known = result.Select(f => f.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var (code, agg) in byCode)
        {
            if (known.Contains(code))
                continue;
            result.Add(new FactFacilityRowInternal
            {
                Code = code,
                Name = agg.Name,
                FactPhysicalVolume = agg.Volume > 0 ? agg.Volume : null,
            });
        }

        return result;
    }

    private static FactDetailRowInternal ToCompareDetailInternal(ProjectsPageDetailRowDto row) =>
        new()
        {
            FacilityCode = row.FacilityCode,
            FacilityName = row.FacilityName,
            DocumentSet = row.DocumentSet,
            PvCode = string.IsNullOrEmpty(row.PvCode) ? row.CatalogCode : row.PvCode,
            PvName = string.IsNullOrEmpty(row.PvName) ? row.CatalogName : row.PvName,
            WorkId = row.WorkId,
            WorkName = row.WorkName,
            IicGroupName = row.IicGroupName,
            IicUnitName = row.IicUnitName,
            Quantity = row.Quantity,
            PlannedLaborHoursBase = row.BaseHours,
            PlannedLaborHoursOptimistic = row.OptimisticHours,
            PlannedLaborHoursPessimistic = row.PessimisticHours,
            FactLaborHours = row.FactLaborHours,
            FactProductivity = row.FactProductivity,
            ZScore = row.ZScore,
            IsEquipment = row.IsEquipment,
            BindingType = row.BindingType,
            CatalogId = row.CatalogId,
            Tag = row.Tag,
            ItemName = row.ItemName,
            WorkComment = row.WorkComment,
            EstimationBasis = row.EstimationBasis,
            IsAssessment = row.IsAssessment,
            Coefficient = row.Coefficient,
            NormProductivity = row.NormProductivity,
            NormStdDev = row.NormStdDev,
        };
}
