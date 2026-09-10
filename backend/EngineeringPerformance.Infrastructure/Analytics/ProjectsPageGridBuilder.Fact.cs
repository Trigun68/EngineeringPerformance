using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

public static partial class ProjectsPageGridBuilder
{
    private readonly record struct HourRollup(decimal Fact, decimal Pes, decimal Base, decimal Opt);

    private static HourRollup OwnHours(PerformanceRowDto row) => new(
        row.FactLaborHours ?? 0m,
        row.PessimisticHours ?? 0m,
        row.BaseHours ?? 0m,
        row.OptimisticHours ?? 0m);

    /// <summary>
    /// Добавляет строки «Итого: …» (корневые) и «ИТОГО» с суммой часов и ФО по ЕИ.
    /// </summary>
    public static IReadOnlyList<PerformanceRowDto> AppendPerformanceTotals(
        IReadOnlyList<PerformanceRowDto> performance)
    {
        var source = performance.Where(r => !r.IsTotal).ToList();
        var childrenByParent = source
            .Where(r => r.WorkDirectionId >= 0 && r.ParentId.HasValue)
            .ToLookup(r => r.ParentId!.Value);
        var roots = source
            .Where(r => r.WorkDirectionId >= 0 && r.ParentId is null)
            .ToList();

        var result = new List<PerformanceRowDto>();

        HourRollup BuildSubtree(PerformanceRowDto row)
        {
            result.Add(row);
            var children = childrenByParent[row.WorkDirectionId].ToList();
            var own = OwnHours(row);
            if (children.Count == 0)
                return own;

            var acc = own;
            foreach (var child in children)
            {
                var childHours = BuildSubtree(child);
                acc = new HourRollup(
                    acc.Fact + childHours.Fact,
                    acc.Pes + childHours.Pes,
                    acc.Base + childHours.Base,
                    acc.Opt + childHours.Opt);
            }

            var foDisplay = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
                CollectSubtree(row, childrenByParent));
            var physicalDisplay = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                CollectSubtree(row, childrenByParent));
            var assessmentDisplay = PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(
                CollectSubtree(row, childrenByParent));
            result.Add(new PerformanceRowDto(
                WorkDirectionId: row.WorkDirectionId,
                WorkDirectionName: $"Итого: {row.WorkDirectionName}",
                WorkDirectionCode: row.WorkDirectionCode,
                ParentId: row.ParentId,
                Level: row.Level,
                NormUnitName: null,
                FactPhysicalVolume: null,
                FactLaborHours: NullIfNonPositive(acc.Fact),
                FactProductivity: null,
                FactEstimate: null,
                NormProductivity: null,
                NormEstimate: null,
                NormStdDev: null,
                ZScore: null,
                Interpretation: null,
                SubtreeHours: null,
                IsTotal: true,
                FactPhysicalVolumeDisplay: foDisplay,
                PessimisticHours: NullIfNonPositive(acc.Pes),
                BaseHours: NullIfNonPositive(acc.Base),
                OptimisticHours: NullIfNonPositive(acc.Opt),
                PhysicalVolumeDisplay: physicalDisplay,
                AssessmentPhysicalVolumeDisplay: assessmentDisplay));

            return acc;
        }

        foreach (var root in roots)
            BuildSubtree(root);

        foreach (var unallocated in source.Where(r => r.WorkDirectionId < 0))
            result.Add(unallocated);

        var rootTotals = result.Where(r => r.IsTotal && r.Level == 0).ToList();
        var hasData = source.Any(r =>
            r.WorkDirectionId >= 0
            && (r.FactLaborHours is > 0
                || r.FactPhysicalVolume is > 0
                || r.PhysicalVolume is > 0
                || r.AssessmentPhysicalVolume is > 0
                || r.PessimisticHours is > 0
                || r.BaseHours is > 0
                || r.OptimisticHours is > 0));
        if (rootTotals.Count > 0 || hasData)
        {
            HourRollup grand;
            if (rootTotals.Count > 0)
            {
                grand = new HourRollup(
                    rootTotals.Sum(r => r.FactLaborHours ?? 0m),
                    rootTotals.Sum(r => r.PessimisticHours ?? 0m),
                    rootTotals.Sum(r => r.BaseHours ?? 0m),
                    rootTotals.Sum(r => r.OptimisticHours ?? 0m));
            }
            else
            {
                var nodes = source.Where(r => r.WorkDirectionId >= 0).ToList();
                grand = new HourRollup(
                    nodes.Sum(r => r.FactLaborHours ?? 0m),
                    nodes.Sum(r => r.PessimisticHours ?? 0m),
                    nodes.Sum(r => r.BaseHours ?? 0m),
                    nodes.Sum(r => r.OptimisticHours ?? 0m));
            }

            var grandFo = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
                source.Where(r => r.WorkDirectionId >= 0));
            var grandPhysical = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                source.Where(r => r.WorkDirectionId >= 0));
            var grandAssessment = PhysicalVolumeByUnitAggregator.FormatAssessmentPhysicalVolume(
                source.Where(r => r.WorkDirectionId >= 0));
            result.Add(new PerformanceRowDto(
                WorkDirectionId: int.MinValue,
                WorkDirectionName: "ИТОГО",
                WorkDirectionCode: string.Empty,
                ParentId: null,
                Level: -1,
                NormUnitName: null,
                FactPhysicalVolume: null,
                FactLaborHours: NullIfNonPositive(grand.Fact),
                FactProductivity: null,
                FactEstimate: null,
                NormProductivity: null,
                NormEstimate: null,
                NormStdDev: null,
                ZScore: null,
                Interpretation: null,
                SubtreeHours: null,
                IsTotal: true,
                FactPhysicalVolumeDisplay: grandFo,
                PessimisticHours: NullIfNonPositive(grand.Pes),
                BaseHours: NullIfNonPositive(grand.Base),
                OptimisticHours: NullIfNonPositive(grand.Opt),
                PhysicalVolumeDisplay: grandPhysical,
                AssessmentPhysicalVolumeDisplay: grandAssessment));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    public static (List<ProjectsPageFacilityRowDto> Facility, List<ProjectsPageDetailRowDto> Detail) BuildFactGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine engine)
    {
        // Марка Excel = PPV.DocumentSet. DocSet.Code — тот же код после '|'; Brand надёжнее, если навигация не загружена.
        static string FactDocSet(ProjectLaborIntensity li)
        {
            if (!string.IsNullOrWhiteSpace(li.Brand))
                return li.Brand.Trim();
            return li.DocSet?.Code ?? string.Empty;
        }

        var laborIntensities = bundle.LaborIntensities
            .Select(li => new FactLaborIntensityRow(
                FactDocSet(li),
                li.WorkDirectionId,
                li.PlannedHours))
            .ToList();

        var normByDirection = performance
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        var input = FactAssessmentEngineMapper.ToInput(
            bundle.PhysicalVolumes,
            bundle.Equipment,
            laborIntensities,
            normByDirection,
            bundle.ClassifierMap,
            bundle.ClaimedWorkDirectionIds);
        var calculated = engine.CalculateFactAssessment(input);
        var (detailInternal, facilityInternal) = FactAssessmentEngineMapper.FromResult(calculated);

        var ppvHoursById = ComputePpvForecastHours(bundle);
        var liveFacility = BuildForecastFacility(bundle, ppvHoursById);
        var planByFacility = liveFacility
            .GroupBy(f => f.FacilityCode)
            .ToDictionary(
                g => g.Key,
                g => g.First());

        var facility = facilityInternal
            .Select(f =>
            {
                planByFacility.TryGetValue(f.Code, out var hours);
                return new ProjectsPageFacilityRowDto(
                    f.Code,
                    f.Name,
                    f.FactLaborHours,
                    f.ZScore,
                    f.Interpretation,
                    BaseHours: hours?.BaseHours,
                    OptimisticHours: hours?.OptimisticHours,
                    PessimisticHours: hours?.PessimisticHours);
            })
            .ToList();

        var existingFacilityCodes = facility.Select(f => f.FacilityCode).ToHashSet();
        foreach (var live in liveFacility.OrderBy(f => f.FacilityCode))
        {
            if (existingFacilityCodes.Contains(live.FacilityCode))
                continue;
            if ((live.BaseHours ?? 0m) <= 0m
                && (live.OptimisticHours ?? 0m) <= 0m
                && (live.PessimisticHours ?? 0m) <= 0m)
                continue;
            facility.Add(new ProjectsPageFacilityRowDto(
                live.FacilityCode,
                live.FacilityName,
                LaborHours: null,
                ZScore: null,
                Interpretation: string.Empty,
                BaseHours: live.BaseHours,
                OptimisticHours: live.OptimisticHours,
                PessimisticHours: live.PessimisticHours));
        }

        // O(1) lookup — иначе FirstOrDefault по тысячам строк на каждую detail-строку
        // подвешивает page-data и приводит к SQL timeout на следующих запросах.
        var pvById = bundle.PhysicalVolumes.ToDictionary(p => p.Id);
        var eqById = bundle.Equipment.ToDictionary(e => e.Id);

        var detail = new List<ProjectsPageDetailRowDto>(detailInternal.Count);
        foreach (var d in detailInternal)
        {
            ProjectPhysicalVolume? pv = null;
            ProjectEquipment? eq = null;
            if (d.RowId is int rowId)
            {
                if (d.IsEquipment)
                    eqById.TryGetValue(rowId, out eq);
                else
                    pvById.TryGetValue(rowId, out pv);
            }

            var dirId = pv?.GetClassifierId()
                ?? eq?.Equipment?.ClassifierId;
            normByDirection.TryGetValue(dirId ?? -1, out var norm);
            var normId = dirId is int did && bundle.NormIdByDirection.TryGetValue(did, out var nid) ? nid : (int?)null;
            var catalogResource = pv?.PhysicalVolumeKfoId.HasValue == true
                ? "physical-volumes-kfo"
                : pv?.PhysicalVolumeBerId.HasValue == true
                    ? "physical-volumes-ber"
                    : eq is not null
                        ? "equipment"
                        : string.Empty;

            var isAssessment = pv is not null
                ? pv.GetIsAssessment() == true
                : eq?.Equipment?.IsAssessment == true;
            var coefficient = pv?.GetCoefficient()
                ?? eq?.Equipment?.Coefficient
                ?? 1m;

            // План. часы — сохранённые LaborIntensity* (ФО и оборудование).
            decimal? baseH = null, optH = null, pesH = null;
            if (!d.IsEquipment && pv is not null && pv.GetIsAssessment() == true)
            {
                if (ppvHoursById.TryGetValue(pv.Id, out var hours))
                    (baseH, optH, pesH) = hours;
            }
            else if (d.IsEquipment && eq is not null && eq.Equipment?.IsAssessment == true)
            {
                (baseH, optH, pesH) = StoredPlanHours.ForEquipment(eq);
            }

            detail.Add(new ProjectsPageDetailRowDto(
                RowId: d.RowId,
                FacilityCode: d.FacilityCode,
                FacilityName: d.FacilityName,
                DocumentSet: d.DocumentSet,
                CatalogCode: d.PvCode,
                CatalogName: d.PvName,
                WorkId: d.WorkId,
                WorkName: d.WorkName,
                IicGroupName: d.IicGroupName,
                IicUnitName: d.IicUnitName,
                Quantity: d.Quantity,
                BaseHours: baseH,
                OptimisticHours: optH,
                PessimisticHours: pesH,
                FactLaborHours: d.FactLaborHours,
                FactProductivity: d.FactProductivity,
                ZScore: d.ZScore,
                Interpretation: d.ZScore.HasValue
                    ? PerformanceInterpretationHelper.GetInterpretation(d.ZScore)?.ToRussian() ?? string.Empty
                    : string.Empty,
                IsEquipment: d.IsEquipment,
                BindingType: d.IsEquipment ? "Оборудование" : "ФО",
                CatalogId: eq?.CatalogId ?? string.Empty,
                Tag: eq?.Tag ?? string.Empty,
                ItemName: eq?.Equipment?.Name ?? eq?.CatalogName ?? d.PvName,
                WorkComment: pv?.WorkComment ?? string.Empty,
                EstimationBasis: eq?.EstimationBasis ?? string.Empty,
                PvCode: d.PvCode,
                PvName: d.PvName,
                IsAssessment: isAssessment ? "Да" : "Нет",
                Coefficient: coefficient,
                NormProductivity: norm?.NormProductivity,
                NormStdDev: norm?.NormStdDev,
                ClassifierId: dirId,
                NormId: normId,
                CatalogVolumeId: pv?.GetCatalogVolumeId(),
                CatalogEquipmentId: eq?.EquipmentId,
                CatalogResource: catalogResource));
        }

        return (facility, detail);
    }
}
