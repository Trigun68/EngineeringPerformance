using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>CPU-сборка 9 гридов из уже загруженного bundle (без доп. SQL).</summary>
public static class ProjectsPageGridBuilder
{
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeLivePpvHours(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = engine;
        return StoredPlanHours.ByPpvId(bundle);
    }

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        ProjectsPageVersionBundle bundle) =>
        StoredPlanHours.ByPpvId(bundle);

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        ProjectsPageVersionBundle bundle) =>
        StoredPlanHours.ByDirection(bundle);

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
            var foDisplay = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
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
                FactPhysicalVolumeDisplay: foDisplay));

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
        var grand = SumAllForecastScenarioHours(bundle);
        if (grand.Base > 0 || grand.Opt > 0 || grand.Pes > 0 || rootTotals.Count > 0)
        {
            var grandFo = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
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
                FactPhysicalVolumeDisplay: grandFo));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    private static List<PerformanceRowDto> CollectSubtree(
        PerformanceRowDto root,
        ILookup<int, PerformanceRowDto> childrenByParent)
    {
        var list = new List<PerformanceRowDto> { root };
        void Walk(int id)
        {
            foreach (var child in childrenByParent[id])
            {
                list.Add(child);
                Walk(child.WorkDirectionId);
            }
        }

        Walk(root.WorkDirectionId);
        return list;
    }

    private readonly record struct HourRollup(decimal Fact, decimal Pes, decimal Base, decimal Opt);

    private static decimal? NullIfNonPositive(decimal value) =>
        value > 0m ? Math.Round(value, 2) : null;

    private static HourRollup OwnHours(PerformanceRowDto row) => new(
        row.FactLaborHours ?? 0m,
        row.PessimisticHours ?? 0m,
        row.BaseHours ?? 0m,
        row.OptimisticHours ?? 0m);

    /// <summary>
    /// Плановые часы для UI: сохранённые LaborIntensity* ФО и оборудования.
    /// </summary>
    public static IReadOnlyList<PerformanceRowDto> AttachLivePlanHours(
        IReadOnlyList<PerformanceRowDto> performance,
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = engine;
        var byDirection = AggregateForecastHoursByDirection(bundle);

        return performance
            .Select(row =>
            {
                if (!byDirection.TryGetValue(row.WorkDirectionId, out var hours))
                    return row;
                return row with
                {
                    PessimisticHours = NullIfNonPositive(hours.Pessimistic ?? 0m),
                    BaseHours = NullIfNonPositive(hours.Base ?? 0m),
                    OptimisticHours = NullIfNonPositive(hours.Optimistic ?? 0m),
                };
            })
            .ToList();
    }

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
                OptimisticHours: NullIfNonPositive(acc.Opt)));

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
                OptimisticHours: NullIfNonPositive(grand.Opt)));
        }

        return result.Where(r => !r.IsTotal || r.Level <= 0).ToList();
    }

    public static IReadOnlyList<PerformanceRowDto> BuildPerformance(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine) =>
        BuildPerformanceFromLabor(
            bundle,
            engine,
            bundle.LaborIntensities
                .Select(li => new LaborIntensityCalculationLine(li.WorkDirectionId, li.PlannedHours))
                .ToList());

    /// <summary>
    /// Z и производительность плана: сохранённые часы выбранного варианта (по умолчанию базовый)
    /// через тот же <see cref="IPerformanceCalculationEngine.CalculatePerformance"/>.
    /// </summary>
    public static IReadOnlyList<PerformanceRowDto> BuildPlanPerformance(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine,
        PlanHourKind kind = PlanHourKind.Base)
    {
        var hoursByDirection = ComputeLivePlanHoursByDirection(bundle);
        var labor = hoursByDirection
            .Select(kv => new LaborIntensityCalculationLine(
                kv.Key,
                SelectPlanHours(kv.Value, kind) ?? 0m))
            .Where(l => l.PlannedHours > 0)
            .ToList();
        return BuildPerformanceFromLabor(bundle, engine, labor);
    }

    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeLivePlanHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = engine;
        return AggregateForecastHoursByDirection(bundle);
    }

    public static decimal? SelectPlanHours(
        (decimal? Base, decimal? Optimistic, decimal? Pessimistic) hours,
        PlanHourKind kind) =>
        kind switch
        {
            PlanHourKind.Optimistic => hours.Optimistic,
            PlanHourKind.Pessimistic => hours.Pessimistic,
            _ => hours.Base,
        };

    private static IReadOnlyList<PerformanceRowDto> BuildPerformanceFromLabor(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine,
        IReadOnlyList<LaborIntensityCalculationLine> labor)
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
            LaborIntensities: labor,
            NormsByDirection: bundle.NormsByDirection);

        var rows = engine.CalculatePerformance(input);
        return ApplyFormattedWorkDirectionNames(rows, bundle.ClassifierMap);
    }

    /// <summary>
    /// Группы IIC в гридах: «код — наименование (ед.)» через <see cref="IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit"/>.
    /// </summary>
    public static IReadOnlyList<PerformanceRowDto> ApplyFormattedWorkDirectionNames(
        IReadOnlyList<PerformanceRowDto> rows,
        IReadOnlyDictionary<int, IicWorkDirection> classifierMap)
    {
        if (rows.Count == 0)
            return rows;

        return rows
            .Select(r =>
            {
                if (r.WorkDirectionId < 0 || !classifierMap.TryGetValue(r.WorkDirectionId, out var dir))
                    return r;
                var formatted = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, classifierMap);
                return r.WorkDirectionName == formatted
                    ? r
                    : r with { WorkDirectionName = formatted };
            })
            .ToList();
    }

    public static (List<ProjectsPageFacilityRowDto> Facility, List<ProjectsPageDetailRowDto> Detail) BuildFactGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine engine)
    {
        static string DocSetCode(ProjectLaborIntensity li) =>
            li.DocSetId is null ? string.Empty : (li.DocSet?.Code ?? string.Empty);

        var laborIntensities = bundle.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .Select(li => new FactLaborIntensityRow(
                DocSetCode(li),
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
            bundle.ClassifierMap);
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

    public static ProjectsPageForecastGridsDto BuildForecastGrids(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = engine;
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

        var hoursA = ResolveScenarioHoursByDirection(bundleA, performanceA, engine);
        var hoursB = ResolveScenarioHoursByDirection(bundleB, performanceB, engine);
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
            VersionCompareMerge.BuildDetailRows(detailA, detailB));
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

            result.Add(new VersionCompareRowDto(
                row.WorkDirectionId,
                row.ParentId,
                $"Итого: {row.Name}",
                FactPhysicalVolumeDisplayA: foA,
                FactPhysicalVolumeDisplayB: foB,
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
            .Any(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.FactPhysicalVolume is > 0);
        if (rootTotals.Count > 0 || hasFo || source.Count > 0)
        {
            var grandFoA = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
                performanceA.Values.Where(r => !r.IsTotal && r.WorkDirectionId >= 0));
            var grandFoB = PhysicalVolumeByUnitAggregator.FormatFromPerformance(
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

    /// <summary>
    /// Сохранённые часы по направлениям (ФО + оборудование). Без live N(V).
    /// </summary>
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ResolveLivePlanHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = performance;
        return ComputeLivePlanHoursByDirection(bundle, engine);
    }

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ResolveScenarioHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance,
        IPerformanceCalculationEngine? engine = null)
    {
        _ = performance;
        return ComputeLivePlanHoursByDirection(bundle, engine);
    }

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
            PlannedLaborHoursA: hoursA.Base,
            PlannedLaborHoursB: hoursB.Base,
            PlannedLaborHoursOptimisticA: hoursA.Optimistic,
            PlannedLaborHoursOptimisticB: hoursB.Optimistic,
            PlannedLaborHoursPessimisticA: hoursA.Pessimistic,
            PlannedLaborHoursPessimisticB: hoursB.Pessimistic,
            PlannedProductivityA: PlannedProductivity(hoursA.Base, a?.FactPhysicalVolume),
            PlannedProductivityB: PlannedProductivity(hoursB.Base, b?.FactPhysicalVolume),
            PlannedProductivityOptimisticA: PlannedProductivity(hoursA.Optimistic, a?.FactPhysicalVolume),
            PlannedProductivityOptimisticB: PlannedProductivity(hoursB.Optimistic, b?.FactPhysicalVolume),
            PlannedProductivityPessimisticA: PlannedProductivity(hoursA.Pessimistic, a?.FactPhysicalVolume),
            PlannedProductivityPessimisticB: PlannedProductivity(hoursB.Pessimistic, b?.FactPhysicalVolume),
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
                g => (
                    Name: g.First().FacilityName,
                    Volume: g.Sum(d => d.Quantity)));

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
