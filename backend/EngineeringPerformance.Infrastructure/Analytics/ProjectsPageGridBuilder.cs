using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>CPU-сборка 9 гридов из уже загруженного bundle (без доп. SQL).</summary>
internal static class ProjectsPageGridBuilder
{
    private static List<ForecastIicVolumeRowDto> BuildForecastSummary(
        IReadOnlyList<PerformanceRowDto> performance,
        IReadOnlyDictionary<int, decimal> volumeByDirection,
        IReadOnlyDictionary<int, decimal> hoursByDirection)
    {
        return performance
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r =>
            {
                var vol = volumeByDirection.GetValueOrDefault(r.WorkDirectionId, r.FactPhysicalVolume ?? 0m);
                var laborHours = hoursByDirection.GetValueOrDefault(r.WorkDirectionId, r.FactLaborHours ?? 0m);
                var (baseH, optH, pesH) = CalcForecastHours(vol, r.NormProductivity, r.NormStdDev);
                if (baseH is null && laborHours > 0)
                    baseH = Math.Round(laborHours, 2);

                var range = optH.HasValue && pesH.HasValue
                    ? $"{optH:0.##} – {pesH:0.##}"
                    : "—";

                return new ForecastIicVolumeRowDto(
                    r.WorkDirectionId,
                    r.ParentId,
                    r.WorkDirectionName,
                    r.NormUnitName,
                    r.NormProductivity,
                    r.NormEstimate,
                    r.NormStdDev,
                    Math.Round(vol, 4),
                    optH ?? 0m,
                    baseH ?? 0m,
                    pesH ?? 0m,
                    range,
                    r.WorkDirectionName);
            })
            .ToList();
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
            .Select(d => new ProjectsPageDetailRowDto(
                RowId: null,
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
                IsEquipment: string.IsNullOrEmpty(d.WorkId) && string.IsNullOrEmpty(d.WorkName)))
            .ToList();

        return (facility, detail);
    }

    public static ProjectsPageForecastGridsDto BuildForecastGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        var volumeByDirection = bundle.PhysicalVolumes
            .Where(pv => pv.GetClassifierId().HasValue == true && pv.GetIsAssessment() == true)
            .GroupBy(pv => pv.GetClassifierId()!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));

        EquipmentVolumeAggregator.MergeInto(volumeByDirection, bundle.Equipment);

        var hoursByDirection = bundle.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.PlannedHours));

        var summary = BuildForecastSummary(performance, volumeByDirection, hoursByDirection);
        var facility = BuildForecastFacility(bundle, performance);
        var detail = BuildForecastDetail(bundle, performance);

        return new ProjectsPageForecastGridsDto(summary, facility, detail);
    }

    private static List<ProjectsPageFacilityRowDto> BuildForecastFacility(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        var normByDirection = performance
            .Where(r => r.NormProductivity.HasValue)
            .ToDictionary(r => r.WorkDirectionId, r => r.NormProductivity!.Value);

        var rows = new List<ProjectsPageFacilityRowDto>();

        foreach (var group in bundle.PhysicalVolumes
                     .Where(pv => pv.GetIsAssessment() == true)
                     .GroupBy(pv => pv.FacilityCode ?? string.Empty))
        {
            decimal hours = 0m;
            foreach (var pv in group)
            {
                var dirId = pv.GetClassifierId();
                if (dirId is int id && normByDirection.TryGetValue(id, out var p))
                    hours += pv.QuantityInIicGroupUnits() * p;
            }

            var first = group.First();
            rows.Add(new ProjectsPageFacilityRowDto(
                group.Key,
                first.FacilityName ?? string.Empty,
                Math.Round(hours, 2),
                null,
                string.Empty));
        }

        foreach (var group in bundle.Equipment
                     .Where(e => e.Equipment?.IsAssessment == true)
                     .GroupBy(e => e.FacilityCode ?? string.Empty))
        {
            decimal hours = 0m;
            foreach (var eq in group)
            {
                var dirId = eq.Equipment?.ClassifierId;
                if (dirId is int id && normByDirection.TryGetValue(id, out var p))
                    hours += eq.QuantityInIicGroupUnits() * p;
            }

            var first = group.First();
            var existing = rows.FindIndex(r => r.FacilityCode == group.Key);
            if (existing >= 0)
            {
                var prev = rows[existing];
                rows[existing] = prev with { LaborHours = (prev.LaborHours ?? 0m) + Math.Round(hours, 2) };
            }
            else
            {
                rows.Add(new ProjectsPageFacilityRowDto(
                    group.Key,
                    first.FacilityName ?? string.Empty,
                    Math.Round(hours, 2),
                    null,
                    string.Empty));
            }
        }

        return rows.OrderBy(r => r.FacilityCode).ToList();
    }

    private static List<ProjectsPageDetailRowDto> BuildForecastDetail(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        var normLookup = performance
            .Where(r => r.NormProductivity.HasValue)
            .ToDictionary(r => r.WorkDirectionId);

        var result = new List<ProjectsPageDetailRowDto>();

        foreach (var pv in bundle.PhysicalVolumes)
        {
            var dirId = pv.GetClassifierId();
            normLookup.TryGetValue(dirId ?? -1, out var norm);
            var vol = pv.QuantityInIicGroupUnits();
            var (baseH, optH, pesH) = CalcForecastHours(vol, norm?.NormProductivity, norm?.NormStdDev);

            result.Add(MapPvForecastRow(pv, norm, baseH, optH, pesH));
        }

        foreach (var eq in bundle.Equipment)
        {
            var dirId = eq.Equipment?.ClassifierId;
            normLookup.TryGetValue(dirId ?? -1, out var norm);
            var vol = eq.QuantityInIicGroupUnits();
            var (baseH, optH, pesH) = CalcForecastHours(vol, norm?.NormProductivity, norm?.NormStdDev);

            result.Add(new ProjectsPageDetailRowDto(
                eq.Id,
                eq.FacilityCode ?? string.Empty,
                eq.FacilityName ?? string.Empty,
                eq.DocumentSet ?? string.Empty,
                eq.CatalogId ?? string.Empty,
                eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                string.Empty,
                string.Empty,
                dirId is int cid && bundle.ClassifierMap.TryGetValue(cid, out var cl)
                    ? $"{cl.Code} {cl.Name}"
                    : string.Empty,
                norm?.NormUnitName ?? string.Empty,
                eq.Quantity,
                baseH,
                optH,
                pesH,
                null,
                null,
                null,
                string.Empty,
                true));
        }

        return result;
    }

    private static ProjectsPageDetailRowDto MapPvForecastRow(
        ProjectPhysicalVolume pv,
        PerformanceRowDto? norm,
        decimal? baseH,
        decimal? optH,
        decimal? pesH)
    {
        var dirId = pv.GetClassifierId();
        return new ProjectsPageDetailRowDto(
            pv.Id,
            pv.FacilityCode ?? string.Empty,
            pv.FacilityName ?? string.Empty,
            pv.DocumentSet ?? string.Empty,
            pv.GetVolumeCode(),
            pv.GetVolumeName(),
            pv.WorkId ?? string.Empty,
            pv.WorkName ?? string.Empty,
            dirId is int cid
                ? $"{pv.GetVolumeCode()} {pv.GetVolumeName()}"
                : string.Empty,
            norm?.NormUnitName ?? string.Empty,
            pv.Quantity,
            baseH,
            optH,
            pesH,
            null,
            null,
            null,
            string.Empty,
            false);
    }

    private static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) CalcForecastHours(
        decimal vol,
        decimal? productivity,
        decimal? stdDev)
    {
        if (vol <= 0 || !productivity.HasValue)
            return (null, null, null);

        var p = productivity.Value;
        var std = stdDev ?? 0m;
        var baseH = Math.Round(vol * p, 2);
        var optH = std > 0 ? Math.Round(vol * Math.Max(0m, p - 0.5m * std), 2) : baseH;
        var pesH = std > 0 ? Math.Round(vol * (p + 0.5m * std), 2) : baseH;
        return (baseH, optH, pesH);
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
