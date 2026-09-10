using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

public static partial class ProjectsPageGridBuilder
{
    private static List<ProjectsPageForecastSummaryRowDto> BuildForecastSummary(
        IReadOnlyList<PerformanceRowDto> performance,
        ProjectsPageVersionBundle bundle)
    {
        var classifierMap = bundle.ClassifierMap;
        var hoursByDirection = AggregateForecastHoursByDirection(bundle);
        string FormatDirectionName(PerformanceRowDto row)
        {
            if (row.WorkDirectionId < 0 || !classifierMap.TryGetValue(row.WorkDirectionId, out var dir))
                return row.WorkDirectionName;
            return IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, classifierMap);
        }

        var perfRows = performance.Where(r => !r.IsTotal).ToList();
        var childrenByParent = perfRows
            .Where(r => r.WorkDirectionId >= 0 && r.ParentId.HasValue)
            .ToLookup(r => r.ParentId!.Value);
        var roots = perfRows
            .Where(r => r.WorkDirectionId >= 0 && !r.ParentId.HasValue)
            .ToList();

        var result = new List<ProjectsPageForecastSummaryRowDto>();

        (decimal? baseH, decimal? optH, decimal? pesH) CalcHoursFromRow(PerformanceRowDto row) =>
            hoursByDirection.TryGetValue(row.WorkDirectionId, out var aggregated)
                ? aggregated
                : (null, null, null);

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
                row.PhysicalVolume,
                row.NormProductivity,
                ownBase,
                ownOpt,
                ownPes,
                false,
                level,
                row.ParentId,
                row.WorkDirectionCode,
                hierarchy,
                PhysicalVolume: row.PhysicalVolume));

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
            var foDisplay = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                CollectSubtree(row, childrenByParent));
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
                new[] { totalLabel },
                FactPhysicalVolumeDisplay: foDisplay,
                PhysicalVolumeDisplay: foDisplay));

            return (aggBase, aggOpt, aggPes);
        }

        foreach (var root in roots)
            BuildSubtree(root, 0, Array.Empty<string>());

        var unallocated = perfRows.FirstOrDefault(r => r.WorkDirectionId < 0);
        if (unallocated is not null)
        {
            var (ub, uo, up) = CalcHoursFromRow(unallocated);
            result.Add(new ProjectsPageForecastSummaryRowDto(
                unallocated.WorkDirectionId,
                unallocated.WorkDirectionName,
                string.Empty,
                unallocated.PhysicalVolume,
                null,
                ub,
                uo,
                up,
                false,
                0,
                null,
                string.Empty,
                new[] { unallocated.WorkDirectionName },
                PhysicalVolume: unallocated.PhysicalVolume));
        }

        var rootTotals = result.Where(r => r.IsTotal && r.Level == 0).ToList();
        var grand = SumAllForecastScenarioHours(bundle);
        if (grand.Base > 0 || grand.Opt > 0 || grand.Pes > 0 || rootTotals.Count > 0)
        {
            var grandFo = PhysicalVolumeByUnitAggregator.FormatPhysicalVolume(
                perfRows.Where(r => r.WorkDirectionId >= 0));
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
                new[] { "ИТОГО" },
                FactPhysicalVolumeDisplay: grandFo,
                PhysicalVolumeDisplay: grandFo));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    public static ProjectsPageForecastGridsDto BuildForecastGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        var ppvHoursById = ComputePpvForecastHours(bundle);
        var summary = BuildForecastSummary(performance, bundle);
        var facility = BuildForecastFacility(bundle, ppvHoursById);
        var detail = BuildForecastDetail(bundle, performance, ppvHoursById);
        var grand = SumAllForecastScenarioHours(bundle);

        return new ProjectsPageForecastGridsDto(
            summary,
            facility,
            detail,
            new ForecastScenarioTotalsDto(grand.Base, grand.Opt, grand.Pes));
    }

    private static (decimal Base, decimal Opt, decimal Pes) SumAllForecastScenarioHours(
        ProjectsPageVersionBundle bundle)
    {
        decimal baseSum = 0m, optSum = 0m, pesSum = 0m;
        var ppvHoursById = ComputePpvForecastHours(bundle);

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
            var hours = StoredPlanHours.ForEquipment(eq);
            baseSum += hours.Base ?? 0m;
            optSum += hours.Optimistic ?? 0m;
            pesSum += hours.Pessimistic ?? 0m;
        }

        return (Math.Round(baseSum, 2), Math.Round(optSum, 2), Math.Round(pesSum, 2));
    }

    private static List<ProjectsPageFacilityRowDto> BuildForecastFacility(
        ProjectsPageVersionBundle bundle,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById)
    {
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
            var hours = StoredPlanHours.ForEquipment(eq);
            var basePart = hours.Base ?? 0m;
            var optPart = hours.Optimistic ?? 0m;
            var pesPart = hours.Pessimistic ?? 0m;

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
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById)
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
            decimal? baseH = null, optH = null, pesH = null;
            if (isAssessment)
                (baseH, optH, pesH) = StoredPlanHours.ForEquipment(eq);

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
}
