using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>CPU-сборка 9 гридов из уже загруженного bundle (без доп. SQL).</summary>
public static class ProjectsPageGridBuilder
{
    private static (decimal? baseH, decimal? optH, decimal? pesH) CalcForecastHours(
        IPerformanceCalculationEngine engine,
        decimal vol,
        decimal effectiveVol,
        int workDirectionId,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z)
    {
        if (normsByDirection.TryGetValue(workDirectionId, out var snap))
            return engine.ComputeForecastHours(snap.NormFormula, snap.DeviationFormula, vol, effectiveVol, z);
        return engine.ComputeForecastHours(vol, productivity, stdDev, z);
    }

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        ProjectsPageVersionBundle bundle)
    {
        var lines = bundle.PhysicalVolumes
            .Where(pv => pv.GetIsAssessment() == true)
            .Select(pv => new LaborIntensityCalculator.PpvLine(
                pv.Id,
                pv.GetClassifierId(),
                pv.FacilityCode,
                pv.Quantity,
                pv.GetCoefficient(),
                IsAssessment: true))
            .ToList();

        if (lines.Count == 0)
            return new Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)>();

        var calculator = new DeviationCalculator(new NormCalculator());
        return ForecastHoursAggregator.ComputePpvHours(
            bundle.PlanningScenario,
            lines,
            bundle.NormsByDirection,
            bundle.PlanningZByDirection,
            calculator);
    }

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById)
    {
        var lines = bundle.PhysicalVolumes
            .Where(pv => pv.GetIsAssessment() == true)
            .Select(pv => new LaborIntensityCalculator.PpvLine(
                pv.Id,
                pv.GetClassifierId(),
                pv.FacilityCode,
                pv.Quantity,
                pv.GetCoefficient(),
                IsAssessment: true))
            .ToList();

        var equipmentVolumes = bundle.Equipment
            .Where(e => e.Equipment?.IsAssessment == true && e.Equipment.ClassifierId.HasValue)
            .Select(e => (e.Equipment!.ClassifierId!.Value, e.QuantityInIicGroupUnits()));

        var calculator = new DeviationCalculator(new NormCalculator());
        return ForecastHoursAggregator.AggregateByDirection(
            lines,
            ppvHoursById,
            equipmentVolumes,
            bundle.NormsByDirection,
            bundle.PlanningZByDirection,
            calculator);
    }

    private static List<ProjectsPageForecastSummaryRowDto> BuildForecastSummary(
        IReadOnlyList<PerformanceRowDto> performance,
        ProjectsPageVersionBundle bundle,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IPerformanceCalculationEngine engine)
    {
        var classifierMap = bundle.ClassifierMap;
        var planningZByDirection = bundle.PlanningZByDirection;
        var normsByDirection = bundle.NormsByDirection;
        var hoursByDirection = AggregateForecastHoursByDirection(bundle, ppvHoursById);
        string FormatDirectionName(PerformanceRowDto row)
        {
            if (row.WorkDirectionId < 0 || !classifierMap.TryGetValue(row.WorkDirectionId, out var dir))
                return row.WorkDirectionName;
            return IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, classifierMap);
        }

        var childrenByParent = performance
            .Where(r => r.WorkDirectionId >= 0 && r.ParentId.HasValue)
            .ToLookup(r => r.ParentId!.Value);
        var roots = performance
            .Where(r => r.WorkDirectionId >= 0 && !r.ParentId.HasValue)
            .ToList();

        var result = new List<ProjectsPageForecastSummaryRowDto>();

        (decimal? baseH, decimal? optH, decimal? pesH) CalcHoursFromRow(PerformanceRowDto row)
        {
            if (hoursByDirection.TryGetValue(row.WorkDirectionId, out var aggregated))
                return aggregated;

            var vol = row.FactPhysicalVolume ?? 0m;
            if (vol <= 0 || !row.NormProductivity.HasValue)
                return (null, null, null);
            var z = ForecastCalculation.ResolvePlanningZ(planningZByDirection, row.WorkDirectionId);
            return CalcForecastHours(engine, vol, vol, row.WorkDirectionId, normsByDirection, row.NormProductivity, row.NormStdDev, z);
        }

        (decimal baseH, decimal optH, decimal pesH) BuildSubtree(
            PerformanceRowDto row,
            int level,
            IReadOnlyList<string> parentPath)
        {
            var (ownBase, ownOpt, ownPes) = CalcHoursFromRow(row);
            var children = childrenByParent[row.WorkDirectionId].ToList();
            var hasChildren = children.Count > 0;
            var displayName = FormatDirectionName(row);
            var hierarchy = parentPath.Concat(new[] { displayName }).ToList();

            result.Add(new ProjectsPageForecastSummaryRowDto(
                row.WorkDirectionId,
                displayName,
                string.Empty,
                row.FactPhysicalVolume,
                row.NormProductivity,
                ownBase,
                ownOpt,
                ownPes,
                false,
                level,
                row.ParentId,
                row.WorkDirectionCode,
                hierarchy));

            if (!hasChildren)
                return (ownBase ?? 0m, ownOpt ?? 0m, ownPes ?? 0m);

            decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
            foreach (var child in children)
            {
                var (cb, co, cp) = BuildSubtree(child, level + 1, hierarchy);
                totalBase += cb;
                totalOpt += co;
                totalPes += cp;
            }

            var aggBase = (ownBase ?? 0m) + totalBase;
            var aggOpt = (ownOpt ?? 0m) + totalOpt;
            var aggPes = (ownPes ?? 0m) + totalPes;

            var totalLabel = $"Итого: {row.WorkDirectionName}";
            result.Add(new ProjectsPageForecastSummaryRowDto(
                row.WorkDirectionId,
                totalLabel,
                string.Empty,
                null,
                null,
                aggBase > 0 ? aggBase : null,
                aggOpt > 0 ? aggOpt : null,
                aggPes > 0 ? aggPes : null,
                true,
                level,
                row.ParentId,
                row.WorkDirectionCode,
                new[] { totalLabel }));

            return (aggBase, aggOpt, aggPes);
        }

        foreach (var root in roots)
            BuildSubtree(root, 0, Array.Empty<string>());

        var unallocated = performance.FirstOrDefault(r => r.WorkDirectionId < 0);
        if (unallocated is not null)
        {
            var (ub, uo, up) = CalcHoursFromRow(unallocated);
            result.Add(new ProjectsPageForecastSummaryRowDto(
                unallocated.WorkDirectionId,
                unallocated.WorkDirectionName,
                string.Empty,
                unallocated.FactPhysicalVolume,
                null,
                ub,
                uo,
                up,
                false,
                0,
                null,
                string.Empty,
                new[] { unallocated.WorkDirectionName }));
        }

        var rootTotals = result.Where(r => r.IsTotal && r.Level == 0).ToList();
        var grand = SumAllForecastScenarioHours(bundle, performance, ppvHoursById, engine);
        if (grand.Base > 0 || grand.Opt > 0 || grand.Pes > 0 || rootTotals.Count > 0)
        {
            result.Add(new ProjectsPageForecastSummaryRowDto(
                int.MinValue,
                "ИТОГО",
                string.Empty,
                null,
                null,
                grand.Base > 0 ? grand.Base : null,
                grand.Opt > 0 ? grand.Opt : null,
                grand.Pes > 0 ? grand.Pes : null,
                true,
                -1,
                null,
                string.Empty,
                new[] { "ИТОГО" }));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    public static IReadOnlyList<PerformanceRowDto> BuildPerformance(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine)
    {
        var directionById = bundle.WorkDirections.ToDictionary(d => d.Id);
        var input = new PerformanceCalculationInput(
            VersionExists: bundle.VersionExists,
            WorkDirections: bundle.WorkDirections
                .Select(d => new WorkDirectionCalculationNode(
                    d.Id,
                    d.ParentId,
                    d.Name,
                    d.Code,
                    IicWorkDirectionDisplayUnit.ResolveUnitName(d, directionById)))
                .ToList(),
            PhysicalVolumes: bundle.PhysicalVolumes
                .Where(pv => pv.GetCatalogVolumeId().HasValue)
                .Select(pv => new PhysicalVolumeCalculationLine(
                    pv.GetClassifierId(),
                    pv.GetIsAssessment() == true,
                    pv.Quantity,
                    pv.GetCoefficient()))
                .ToList(),
            Equipment: bundle.Equipment
                .Select(e => new EquipmentCalculationLine(
                    e.Equipment?.ClassifierId,
                    e.Equipment?.IsAssessment == true,
                    e.Quantity,
                    e.Equipment?.Coefficient ?? 1m))
                .ToList(),
            LaborIntensities: bundle.LaborIntensities
                .Select(li => new LaborIntensityCalculationLine(li.WorkDirectionId, li.PlannedHours))
                .ToList(),
            NormsByDirection: bundle.NormsByDirection);

        return engine.CalculatePerformance(input);
    }

    public static (List<ProjectsPageFacilityRowDto> Facility, List<ProjectsPageDetailRowDto> Detail) BuildFactGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        static string ExtractDocSet(string? code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            var pipeIdx = code.IndexOf('|');
            return pipeIdx >= 0 ? code[(pipeIdx + 1)..].Trim() : string.Empty;
        }

        var laborIntensities = bundle.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .Select(li => new FactLaborIntensityRow(
                ExtractDocSet(li.DocumentationCode),
                li.WorkDirectionId,
                li.PlannedHours))
            .ToList();

        var normByDirection = performance
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        var (detailInternal, facilityInternal) = FactAssessmentRowsCalculator.Compute(
            bundle.PhysicalVolumes,
            bundle.Equipment,
            laborIntensities,
            normByDirection,
            bundle.ClassifierMap);

        var facility = facilityInternal
            .Select(f => new ProjectsPageFacilityRowDto(
                f.Code,
                f.Name,
                f.FactLaborHours,
                f.ZScore,
                f.Interpretation))
            .ToList();

        var detail = detailInternal
            .Select(d =>
            {
                var dirId = bundle.PhysicalVolumes.FirstOrDefault(pv => pv.Id == d.RowId)?.GetClassifierId()
                    ?? bundle.Equipment.FirstOrDefault(eq => eq.Id == d.RowId)?.Equipment?.ClassifierId;
                normByDirection.TryGetValue(dirId ?? -1, out var norm);
                var normId = dirId is int did && bundle.NormIdByDirection.TryGetValue(did, out var nid) ? nid : (int?)null;
                var pv = bundle.PhysicalVolumes.FirstOrDefault(p => p.Id == d.RowId);
                var eq = bundle.Equipment.FirstOrDefault(e => e.Id == d.RowId);
                var catalogResource = pv?.PhysicalVolumeKfoId.HasValue == true
                    ? "physical-volumes-kfo"
                    : pv?.PhysicalVolumeBerId.HasValue == true
                        ? "physical-volumes-ber"
                        : string.Empty;
                return new ProjectsPageDetailRowDto(
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
                    BaseHours: null,
                    OptimisticHours: null,
                    PessimisticHours: null,
                    FactLaborHours: d.FactLaborHours,
                    FactProductivity: d.FactProductivity,
                    ZScore: d.ZScore,
                    Interpretation: d.ZScore.HasValue
                        ? PerformanceInterpretationHelper.GetInterpretation(d.ZScore)?.ToRussian() ?? string.Empty
                        : string.Empty,
                    IsEquipment: d.IsEquipment,
                    PvCode: d.PvCode,
                    PvName: d.PvName,
                    NormProductivity: norm?.NormProductivity,
                    ClassifierId: dirId,
                    NormId: normId,
                    CatalogVolumeId: pv?.GetCatalogVolumeId(),
                    CatalogEquipmentId: eq?.EquipmentId,
                    CatalogResource: catalogResource);
            })
            .ToList();

        return (facility, detail);
    }

    public static ProjectsPageForecastGridsDto BuildForecastGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine engine)
    {
        var ppvHoursById = ComputePpvForecastHours(bundle);
        var summary = BuildForecastSummary(performance, bundle, ppvHoursById, engine);
        var facility = BuildForecastFacility(bundle, performance, ppvHoursById, engine);
        var detail = BuildForecastDetail(bundle, performance, ppvHoursById, engine);
        var grand = SumAllForecastScenarioHours(bundle, performance, ppvHoursById, engine);

        return new ProjectsPageForecastGridsDto(
            summary,
            facility,
            detail,
            new ForecastScenarioTotalsDto(grand.Base, grand.Opt, grand.Pes));
    }

    private static (decimal Base, decimal Opt, decimal Pes) SumAllForecastScenarioHours(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IPerformanceCalculationEngine engine)
    {
        var normByDirection = performance
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .ToDictionary(r => r.WorkDirectionId);

        decimal baseSum = 0m, optSum = 0m, pesSum = 0m;

        foreach (var pv in bundle.PhysicalVolumes.Where(pv => pv.GetIsAssessment() == true))
        {
            if (!ppvHoursById.TryGetValue(pv.Id, out var hours))
                continue;
            baseSum += hours.Base ?? 0m;
            optSum += hours.Optimistic ?? 0m;
            pesSum += hours.Pessimistic ?? 0m;
        }

        foreach (var eq in bundle.Equipment.Where(e => e.Equipment?.IsAssessment == true))
        {
            var dirId = eq.Equipment?.ClassifierId;
            if (dirId is not int id || !normByDirection.TryGetValue(id, out var norm))
                continue;

            var vol = eq.QuantityInIicGroupUnits();
            var z = ForecastCalculation.ResolvePlanningZ(bundle.PlanningZByDirection, id);
            var (b, o, p) = CalcForecastHours(
                engine, vol, vol, id, bundle.NormsByDirection, norm.NormProductivity, norm.NormStdDev, z);
            baseSum += b ?? 0m;
            optSum += o ?? 0m;
            pesSum += p ?? 0m;
        }

        return (Math.Round(baseSum, 2), Math.Round(optSum, 2), Math.Round(pesSum, 2));
    }

    private static List<ProjectsPageFacilityRowDto> BuildForecastFacility(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IPerformanceCalculationEngine engine)
    {
        var normByDirection = performance
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .ToDictionary(r => r.WorkDirectionId);

        var rows = new Dictionary<(string Code, string Name), (decimal Base, decimal Opt, decimal Pes)>();

        foreach (var pv in bundle.PhysicalVolumes.Where(pv => pv.GetIsAssessment() == true))
        {
            var key = (Code: pv.FacilityCode ?? string.Empty, Name: pv.FacilityName ?? string.Empty);
            if (!ppvHoursById.TryGetValue(pv.Id, out var hours))
                continue;

            if (!rows.TryGetValue(key, out var acc))
                acc = (0m, 0m, 0m);

            rows[key] = (
                acc.Base + (hours.Base ?? 0m),
                acc.Opt + (hours.Optimistic ?? 0m),
                acc.Pes + (hours.Pessimistic ?? 0m));
        }

        foreach (var eq in bundle.Equipment.Where(e => e.Equipment?.IsAssessment == true))
        {
            var key = (Code: eq.FacilityCode ?? string.Empty, Name: eq.FacilityName ?? string.Empty);
            var dirId = eq.Equipment?.ClassifierId;
            if (dirId is not int id || !normByDirection.TryGetValue(id, out var norm))
                continue;

            var vol = eq.QuantityInIicGroupUnits();
            var z = ForecastCalculation.ResolvePlanningZ(bundle.PlanningZByDirection, id);
            var (b, o, p) = CalcForecastHours(engine, vol, vol, id, bundle.NormsByDirection, norm.NormProductivity, norm.NormStdDev, z);
            var basePart = b ?? 0m;
            var optPart = o ?? 0m;
            var pesPart = p ?? 0m;

            if (rows.TryGetValue(key, out var prev))
                rows[key] = (prev.Base + basePart, prev.Opt + optPart, prev.Pes + pesPart);
            else
                rows[key] = (basePart, optPart, pesPart);
        }

        return rows
            .OrderBy(kv => kv.Key.Code)
            .ThenBy(kv => kv.Key.Name)
            .Select(kv => new ProjectsPageFacilityRowDto(
                kv.Key.Code,
                kv.Key.Name,
                null,
                null,
                string.Empty,
                Math.Round(kv.Value.Base, 2),
                Math.Round(kv.Value.Opt, 2),
                Math.Round(kv.Value.Pes, 2)))
            .ToList();
    }

    private static List<ProjectsPageDetailRowDto> BuildForecastDetail(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IPerformanceCalculationEngine engine)
    {
        var normLookup = performance
            .Where(r => r.NormProductivity.HasValue)
            .ToDictionary(r => r.WorkDirectionId);

        var result = new List<ProjectsPageDetailRowDto>();

        foreach (var pv in bundle.PhysicalVolumes
                     .OrderBy(pv => pv.FacilityCode ?? string.Empty)
                     .ThenBy(pv => pv.FacilityName ?? string.Empty)
                     .ThenBy(pv => pv.GetVolumeCode()))
        {
            var dirId = pv.GetClassifierId();
            normLookup.TryGetValue(dirId ?? -1, out var norm);
            var isAssessment = pv.GetIsAssessment() == true;
            decimal? baseH = null, optH = null, pesH = null;
            if (isAssessment && ppvHoursById.TryGetValue(pv.Id, out var hours))
                (baseH, optH, pesH) = hours;

            var normId = dirId is int did && bundle.NormIdByDirection.TryGetValue(did, out var nid) ? nid : (int?)null;
            var catalogResource = pv.PhysicalVolumeKfoId.HasValue
                ? "physical-volumes-kfo"
                : pv.PhysicalVolumeBerId.HasValue
                    ? "physical-volumes-ber"
                    : string.Empty;

            result.Add(MapPvForecastRow(
                pv,
                norm,
                baseH,
                optH,
                pesH,
                bundle.ClassifierMap,
                normId,
                pv.GetCatalogVolumeId(),
                catalogResource));
        }

        foreach (var eq in bundle.Equipment
                     .OrderBy(e => e.FacilityCode ?? string.Empty)
                     .ThenBy(e => e.FacilityName ?? string.Empty)
                     .ThenBy(e => e.Equipment?.Name ?? string.Empty))
        {
            var dirId = eq.Equipment?.ClassifierId;
            normLookup.TryGetValue(dirId ?? -1, out var norm);
            var isAssessment = eq.Equipment?.IsAssessment == true;
            var vol = eq.QuantityInIicGroupUnits();
            decimal? baseH = null, optH = null, pesH = null;
            if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
            {
                var dirKey = dirId ?? -1;
                var z = ForecastCalculation.ResolvePlanningZ(bundle.PlanningZByDirection, dirKey);
                (baseH, optH, pesH) = CalcForecastHours(engine, vol, vol, dirId ?? -1, bundle.NormsByDirection, p, norm.NormStdDev, z);
            }

            var normId = dirId is int did && bundle.NormIdByDirection.TryGetValue(did, out var nid) ? nid : (int?)null;
            var iicGroupName = dirId is int cid && bundle.ClassifierMap.TryGetValue(cid, out var cl)
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, bundle.ClassifierMap)
                : string.Empty;

            result.Add(new ProjectsPageDetailRowDto(
                eq.Id,
                eq.FacilityCode ?? string.Empty,
                eq.FacilityName ?? string.Empty,
                eq.DocumentSet ?? string.Empty,
                eq.CatalogId ?? string.Empty,
                eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                string.Empty,
                string.Empty,
                iicGroupName,
                norm?.NormUnitName ?? string.Empty,
                eq.Quantity,
                baseH,
                optH,
                pesH,
                null,
                null,
                null,
                string.Empty,
                true,
                BindingType: string.Empty,
                CatalogId: eq.CatalogId ?? string.Empty,
                Tag: eq.Tag ?? string.Empty,
                ItemName: eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                EstimationBasis: eq.EstimationBasis ?? string.Empty,
                PvCode: eq.CatalogId ?? string.Empty,
                PvName: eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                IsAssessment: isAssessment ? "Да" : "Нет",
                Coefficient: eq.Equipment?.Coefficient ?? 1m,
                NormProductivity: norm?.NormProductivity,
                NormStdDev: norm?.NormStdDev,
                ClassifierId: dirId,
                NormId: normId,
                CatalogEquipmentId: eq.EquipmentId,
                CatalogResource: "equipment"));
        }

        return result;
    }

    private static ProjectsPageDetailRowDto MapPvForecastRow(
        ProjectPhysicalVolume pv,
        PerformanceRowDto? norm,
        decimal? baseH,
        decimal? optH,
        decimal? pesH,
        IReadOnlyDictionary<int, IicWorkDirection> classifierMap,
        int? normId,
        int? catalogVolumeId,
        string catalogResource)
    {
        var dirId = pv.GetClassifierId();
        var isAssessment = pv.GetIsAssessment() == true;
        var iicGroupName = dirId is int cid && classifierMap.TryGetValue(cid, out var cl)
            ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, classifierMap)
            : string.Empty;
        var pvCode = pv.GetVolumeCode();
        var pvName = pv.GetVolumeName();

        return new ProjectsPageDetailRowDto(
            pv.Id,
            pv.FacilityCode ?? string.Empty,
            pv.FacilityName ?? string.Empty,
            pv.DocumentSet ?? string.Empty,
            pvCode,
            pvName,
            pv.WorkId ?? string.Empty,
            pv.WorkName ?? string.Empty,
            iicGroupName,
            IicWorkDirectionDisplayUnit.ResolveProjectPhysicalVolumeUnitName(pv) ?? norm?.NormUnitName ?? string.Empty,
            pv.Quantity,
            baseH,
            optH,
            pesH,
            null,
            null,
            null,
            string.Empty,
            false,
            BindingType: pv.BindingType ?? string.Empty,
            CatalogId: pv.CatalogId ?? string.Empty,
            Tag: pv.Tag ?? string.Empty,
            ItemName: pv.WorkName ?? string.Empty,
            WorkComment: pv.WorkComment ?? string.Empty,
            EstimationBasis: pv.EstimationBasis ?? string.Empty,
            PvCode: pvCode,
            PvName: pvName,
            IsAssessment: isAssessment ? "Да" : "Нет",
            Coefficient: pv.GetCoefficient(),
            NormProductivity: norm?.NormProductivity,
            NormStdDev: norm?.NormStdDev,
            ClassifierId: dirId,
            NormId: normId,
            CatalogVolumeId: catalogVolumeId,
            CatalogResource: catalogResource);
    }

    public static ProjectsPageCompareGridsDto BuildCompareGrids(
        IReadOnlyList<PerformanceRowDto> performanceA,
        IReadOnlyList<PerformanceRowDto> performanceB,
        ProjectsPageFactGridsDto factA,
        ProjectsPageFactGridsDto factB)
    {
        var dictA = performanceA.GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var dictB = performanceB.GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var allIds = dictA.Keys.Union(dictB.Keys).ToList();

        var summary = new List<VersionCompareRowDto>(allIds.Count);
        foreach (var id in allIds)
        {
            dictA.TryGetValue(id, out var a);
            dictB.TryGetValue(id, out var b);
            if (a is null && b is null) continue;

            var row = a ?? b!;
            decimal? zDelta = a?.ZScore.HasValue == true && b?.ZScore.HasValue == true
                ? b.ZScore - a.ZScore
                : null;

            summary.Add(new VersionCompareRowDto(
                row.WorkDirectionId,
                row.ParentId,
                row.WorkDirectionName,
                a?.ZScore,
                b?.ZScore,
                zDelta,
                a?.Interpretation.ToRussian() is { Length: > 0 } ia ? ia : "-",
                b?.Interpretation.ToRussian() is { Length: > 0 } ib ? ib : "-"));
        }

        var facilityA = factA.Facility
            .Select(f => new FactFacilityRowInternal
            {
                Code = f.FacilityCode,
                Name = f.FacilityName,
                FactLaborHours = f.LaborHours,
                ZScore = f.ZScore,
                Interpretation = f.Interpretation,
            })
            .ToList();

        var facilityB = factB.Facility
            .Select(f => new FactFacilityRowInternal
            {
                Code = f.FacilityCode,
                Name = f.FacilityName,
                FactLaborHours = f.LaborHours,
                ZScore = f.ZScore,
                Interpretation = f.Interpretation,
            })
            .ToList();

        var detailA = factA.Detail.Select(ToCompareDetailInternal).ToList();
        var detailB = factB.Detail.Select(ToCompareDetailInternal).ToList();

        return new ProjectsPageCompareGridsDto(
            summary,
            VersionCompareMerge.BuildFacilityRows(facilityA, facilityB),
            VersionCompareMerge.BuildDetailRows(detailA, detailB));
    }

    private static FactDetailRowInternal ToCompareDetailInternal(ProjectsPageDetailRowDto row) =>
        new()
        {
            FacilityCode = row.FacilityCode,
            FacilityName = row.FacilityName,
            DocumentSet = row.DocumentSet,
            PvCode = row.CatalogCode,
            PvName = row.CatalogName,
            WorkId = row.WorkId,
            WorkName = row.WorkName,
            IicGroupName = row.IicGroupName,
            IicUnitName = row.IicUnitName,
            Quantity = row.Quantity,
            FactLaborHours = row.FactLaborHours,
            FactProductivity = row.FactProductivity,
            ZScore = row.ZScore,
        };
}
