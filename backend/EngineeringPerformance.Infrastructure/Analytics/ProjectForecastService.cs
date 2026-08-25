using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class ProjectForecastService : IProjectForecastService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectForecastService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ProjectForecastReportDto> GenerateForecastAsync(
        int projectId,
        int versionId,
        IReadOnlyDictionary<int, decimal>? nodePlannedTotalsByWorkDirectionId = null,
        ForecastCalculationOptionsDto? calculation = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Include(v => v.Project)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId);

        if (version is null)
        {
            throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");
        }

        var physicalVolumes = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var equipmentRows = await context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        // V2-B.1: forecast строится на снимке норм версии. Раньше runtime-фильтр
        // по EffectiveFrom/EffectiveTo + g.First() давал недетерминированный выбор
        // нормы при перекрытиях; теперь снимок уникален по (Version, WorkDirection).
        var snapshotNorms = await context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .Include(s => s.Norm)
            .Select(s => s.Norm)
            .ToListAsync();

        var workDirections = await context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();

        var normByDirection = snapshotNorms
            .Where(n => n is not null)
            .GroupBy(n => n!.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First()!);

        var ppvHoursById = physicalVolumes
            .Where(pv => pv.GetIsAssessment() == true)
            .ToDictionary(
                pv => pv.Id,
                pv => (pv.LaborIntensityBase, pv.LaborIntensityOptimistic, pv.LaborIntensityPessimistic));

        var scenarioHoursByDirection = new Dictionary<int, (decimal Base, decimal Optimistic, decimal Pessimistic)>();

        void AddHours(int dirId, decimal? b, decimal? o, decimal? p)
        {
            if (!scenarioHoursByDirection.TryGetValue(dirId, out var acc))
                acc = (0m, 0m, 0m);
            scenarioHoursByDirection[dirId] = (
                acc.Base + (b ?? 0m),
                acc.Optimistic + (o ?? 0m),
                acc.Pessimistic + (p ?? 0m));
        }

        var ppvById = physicalVolumes.ToDictionary(pv => pv.Id);
        foreach (var (ppvId, hours) in ppvHoursById)
        {
            if (!ppvById.TryGetValue(ppvId, out var pv) || pv.GetClassifierId() is not int dirId)
                continue;
            AddHours(dirId, hours.LaborIntensityBase, hours.LaborIntensityOptimistic, hours.LaborIntensityPessimistic);
        }

        foreach (var eq in equipmentRows.Where(e => e.Equipment?.IsAssessment == true && e.Equipment.ClassifierId.HasValue))
            AddHours(
                eq.Equipment!.ClassifierId!.Value,
                eq.LaborIntensityBase,
                eq.LaborIntensityOptimistic,
                eq.LaborIntensityPessimistic);

        var scenarioHoursNullable = scenarioHoursByDirection.ToDictionary(
            kv => kv.Key,
            kv => ((decimal?)kv.Value.Base, (decimal?)kv.Value.Optimistic, (decimal?)kv.Value.Pessimistic));

        var volumeByDirection = physicalVolumes
            .Where(pv => pv.GetClassifierId().HasValue == true && pv.GetIsAssessment() == true)
            .GroupBy(pv => pv.GetClassifierId()!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));

        EquipmentVolumeAggregator.MergeInto(volumeByDirection, equipmentRows);

        if (nodePlannedTotalsByWorkDirectionId is not null)
        {
            var direct = ComputeDirectVolumesFromNodeTotals(workDirections, nodePlannedTotalsByWorkDirectionId);
            foreach (var kv in direct)
            {
                volumeByDirection[kv.Key] = kv.Value;
            }
        }

        var calc = calculation ?? new ForecastCalculationOptionsDto();

        var plannedTree = BuildIicPlannedVolumeTree(
            workDirections,
            normByDirection,
            volumeByDirection,
            scenarioHoursNullable);

        var scenarioFlatRows = BuildScenarioDisciplineRows(plannedTree, workDirections);

        var scenarios = BuildScenarios(scenarioFlatRows, calc);

        var top3Disciplines = scenarioFlatRows
            .OrderByDescending(d => d.BaseHours)
            .Take(3)
            .Select((d, index) => new TopDisciplineDto(
                Rank: index + 1,
                DisciplineName: d.HierarchyPath,
                BaseHours: d.BaseHours))
            .ToList();

        var interpretations = BuildInterpretations(scenarios);

        var characteristics = BuildProjectCharacteristics(version.Project);

        return new ProjectForecastReportDto(
            ProjectName: version.Project.Name,
            ProjectCode: version.Project.Code,
            VersionName: version.VersionName,
            ProjectCharacteristics: characteristics,
            PlannedVolumes: plannedTree,
            Scenarios: scenarios,
            Top3Disciplines: top3Disciplines,
            Interpretations: interpretations);
    }

    /// <summary>
    /// По отображаемым в таблице итогам объёма по каждому узлу (листьям и родителям) вычисляет
    /// «прямые» объёмы, которые хранятся по направлениям ИИС и суммируются вверх по дереву.
    /// </summary>
    private static Dictionary<int, decimal> ComputeDirectVolumesFromNodeTotals(
        IReadOnlyList<IicWorkDirection> workDirections,
        IReadOnlyDictionary<int, decimal> nodeTotalByWorkDirectionId)
    {
        var childrenByParent = workDirections
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList());

        var direct = new Dictionary<int, decimal>();

        void Visit(int id)
        {
            if (!childrenByParent.TryGetValue(id, out var children))
            {
                direct[id] = nodeTotalByWorkDirectionId.GetValueOrDefault(id, 0m);
                return;
            }

            foreach (var c in children)
            {
                Visit(c.Id);
            }

            var nodeTotal = nodeTotalByWorkDirectionId.GetValueOrDefault(id, 0m);
            var childSum = children.Sum(c => nodeTotalByWorkDirectionId.GetValueOrDefault(c.Id, 0m));
            direct[id] = Math.Max(0m, nodeTotal - childSum);
        }

        foreach (var root in workDirections.Where(d => d.ParentId == null).OrderBy(d => d.SortOrder).ThenBy(d => d.Name))
        {
            Visit(root.Id);
        }

        return direct;
    }

    private static IReadOnlyList<ForecastIicVolumeRowDto> BuildIicPlannedVolumeTree(
        IReadOnlyList<IicWorkDirection> workDirections,
        IReadOnlyDictionary<int, IicWorkDirectionNorm> normByDirection,
        IReadOnlyDictionary<int, decimal> volumeByDirection,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> scenarioHoursByDirection)
    {
        var byId = workDirections.ToDictionary(d => d.Id);
        var childrenByParent = workDirections
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList());

        var directVolume = workDirections.ToDictionary(d => d.Id, _ => 0m);
        foreach (var kv in volumeByDirection)
        {
            if (byId.ContainsKey(kv.Key))
            {
                directVolume[kv.Key] = kv.Value;
            }
        }

        var totalVolume = new Dictionary<int, decimal>(directVolume);
        // Защита от циклов в иерархии IIC: без HashSet рекурсия уйдёт в StackOverflow,
        // а оборванная транзакция в ForecastPlannedVolumePersistenceService окажется незакрытой.
        var aggregatedIds = new HashSet<int>();

        void AggregateVolumeUp(int id)
        {
            if (!aggregatedIds.Add(id))
            {
                return;
            }

            if (!childrenByParent.TryGetValue(id, out var children))
            {
                return;
            }

            var sum = totalVolume.GetValueOrDefault(id);
            foreach (var c in children)
            {
                AggregateVolumeUp(c.Id);
                sum += totalVolume.GetValueOrDefault(c.Id);
            }

            totalVolume[id] = sum;
        }

        foreach (var root in workDirections.Where(d => d.ParentId == null))
        {
            AggregateVolumeUp(root.Id);
        }

        var hasChildren = workDirections.Where(d => d.ParentId.HasValue)
            .Select(d => d.ParentId!.Value)
            .ToHashSet();

        var results = new List<ForecastIicVolumeRowDto>();
        var walkedIds = new HashSet<int>();
        var leafHoursVisitedIds = new HashSet<int>();

        (decimal Base, decimal Opt, decimal Pes) GetLeafScenarioHours(int id)
        {
            if (!leafHoursVisitedIds.Add(id))
                return (0m, 0m, 0m);

            if (!byId.ContainsKey(id))
                return (0m, 0m, 0m);

            if (!hasChildren.Contains(id))
            {
                if (scenarioHoursByDirection.TryGetValue(id, out var scenarioHours)
                    && ((scenarioHours.Base ?? 0m) > 0
                        || (scenarioHours.Optimistic ?? 0m) > 0
                        || (scenarioHours.Pessimistic ?? 0m) > 0))
                {
                    return (
                        scenarioHours.Base ?? 0m,
                        scenarioHours.Optimistic ?? scenarioHours.Base ?? 0m,
                        scenarioHours.Pessimistic ?? scenarioHours.Base ?? 0m);
                }

                return (0m, 0m, 0m);
            }

            decimal sumBase = 0m, sumOpt = 0m, sumPes = 0m;
            foreach (var c in childrenByParent.GetValueOrDefault(id) ?? [])
            {
                var (cb, co, cp) = GetLeafScenarioHours(c.Id);
                sumBase += cb;
                sumOpt += co;
                sumPes += cp;
            }

            return (sumBase, sumOpt, sumPes);
        }

        void Walk(IicWorkDirection d, string pathPrefix)
        {
            if (!walkedIds.Add(d.Id))
            {
                return;
            }

            var displayName = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(d, byId);
            var path = string.IsNullOrEmpty(pathPrefix) ? displayName : $"{pathPrefix} / {displayName}";
            normByDirection.TryGetValue(d.Id, out var norm);
            var unitName = IicWorkDirectionDisplayUnit.ResolveUnitName(d, byId);

            var vol = totalVolume.GetValueOrDefault(d.Id);

            decimal baseHours;
            decimal opt;
            decimal pes;
            string range;

            var isLeaf = !hasChildren.Contains(d.Id);
            if (isLeaf)
            {
                if (scenarioHoursByDirection.TryGetValue(d.Id, out var scenarioHours)
                    && ((scenarioHours.Base ?? 0m) > 0
                        || (scenarioHours.Optimistic ?? 0m) > 0
                        || (scenarioHours.Pessimistic ?? 0m) > 0))
                {
                    baseHours = Math.Round(scenarioHours.Base ?? 0m, 2);
                    opt = Math.Round(scenarioHours.Optimistic ?? scenarioHours.Base ?? 0m, 2);
                    pes = Math.Round(scenarioHours.Pessimistic ?? scenarioHours.Base ?? 0m, 2);
                    range = baseHours > 0 ? $"{opt:F2} - {pes:F2}" : "—";
                }
                else
                {
                    baseHours = 0m;
                    opt = 0m;
                    pes = 0m;
                    range = "—";
                }
            }
            else
            {
                var (cb, co, cp) = GetLeafScenarioHours(d.Id);
                baseHours = Math.Round(cb, 2);
                opt = Math.Round(co, 2);
                pes = Math.Round(cp, 2);
                range = baseHours > 0 ? $"{opt:F2} - {pes:F2}" : "—";
            }

            results.Add(new ForecastIicVolumeRowDto(
                WorkDirectionId: d.Id,
                ParentId: d.ParentId,
                Name: displayName,
                UnitName: unitName,
                NormProductivity: norm?.Performance,
                NormEstimate: norm?.Estimate,
                NormStandardDeviation: norm?.StandardDeviation,
                PlannedVolume: Math.Round(vol, 4),
                OptimisticHours: opt,
                BaseHours: baseHours,
                PessimisticHours: pes,
                Range: range,
                HierarchyPath: path));

            foreach (var child in childrenByParent.GetValueOrDefault(d.Id) ?? [])
            {
                Walk(child, path);
            }
        }

        foreach (var root in workDirections.Where(d => d.ParentId == null).OrderBy(d => d.SortOrder).ThenBy(d => d.Name))
        {
            Walk(root, string.Empty);
        }

        return results;
    }

    /// <summary>Строки для сценариев: только листья дерева с ненулевой базой или все листья с объёмом/часами.</summary>
    private static List<ForecastIicVolumeRowDto> BuildScenarioDisciplineRows(
        IReadOnlyList<ForecastIicVolumeRowDto> treeRows,
        IReadOnlyList<IicWorkDirection> workDirections)
    {
        var hasChildren = workDirections.Where(d => d.ParentId.HasValue)
            .Select(d => d.ParentId!.Value)
            .ToHashSet();

        return treeRows
            .Where(r => !hasChildren.Contains(r.WorkDirectionId) && (r.BaseHours > 0 || r.PlannedVolume > 0))
            .Select(r => r with { })
            .ToList();
    }

    private static IReadOnlyList<ForecastScenarioDto> BuildScenarios(
        IReadOnlyList<ForecastIicVolumeRowDto> disciplines,
        ForecastCalculationOptionsDto calculation)
    {
        var optimisticTotal = disciplines.Sum(d => d.OptimisticHours);
        var baseTotal = disciplines.Sum(d => d.BaseHours);
        var pessimisticTotal = disciplines.Sum(d => d.PessimisticHours);

        ForecastIicVolumeRowDto MapRow(ForecastIicVolumeRowDto d, decimal o, decimal b, decimal p, string range) =>
            new(
                d.WorkDirectionId,
                d.ParentId,
                d.Name,
                d.UnitName,
                d.NormProductivity,
                d.NormEstimate,
                d.NormStandardDeviation,
                d.PlannedVolume,
                o,
                b,
                p,
                range,
                d.HierarchyPath);

        return new List<ForecastScenarioDto>
        {
            new(
                ScenarioName: "Оптимистичный",
                ZScore: calculation.ZOptimistic,
                TotalLaborHours: optimisticTotal,
                Disciplines: disciplines.Select(d => MapRow(
                    d,
                    d.OptimisticHours,
                    d.BaseHours,
                    d.PessimisticHours,
                    d.Range)).ToList()),
            new(
                ScenarioName: "Базовый",
                ZScore: calculation.ZBase,
                TotalLaborHours: baseTotal,
                Disciplines: disciplines.Select(d => MapRow(
                    d,
                    d.OptimisticHours,
                    d.BaseHours,
                    d.PessimisticHours,
                    d.Range)).ToList()),
            new(
                ScenarioName: "Пессимистичный",
                ZScore: calculation.ZPessimistic,
                TotalLaborHours: pessimisticTotal,
                Disciplines: disciplines.Select(d => MapRow(
                    d,
                    d.OptimisticHours,
                    d.BaseHours,
                    d.PessimisticHours,
                    d.Range)).ToList()),
        };
    }

    private static IReadOnlyList<ScenarioInterpretationDto> BuildInterpretations(IReadOnlyList<ForecastScenarioDto> scenarios)
    {
        var baseScenario = scenarios.First(s => s.ScenarioName == "Базовый");
        var baseTotal = baseScenario.TotalLaborHours;

        var results = new List<ScenarioInterpretationDto>();

        foreach (var scenario in scenarios)
        {
            var percentOfBase = baseTotal > 0
                ? Math.Round(scenario.TotalLaborHours / baseTotal * 100m, 1)
                : 0m;

            var interpretation = scenario.ScenarioName switch
            {
                "Оптимистичный" => "Трудозатраты ниже базового сценария — благоприятный прогноз",
                "Базовый" => "Трудозатраты соответствуют плановым значениям",
                "Пессимистичный" => "Трудозатраты превышают базовый сценарий — требуется резерв",
                _ => "Нет данных"
            };

            results.Add(new ScenarioInterpretationDto(
                ScenarioName: scenario.ScenarioName,
                LaborHours: scenario.TotalLaborHours,
                PercentOfBase: percentOfBase,
                Interpretation: interpretation));
        }

        return results;
    }

    private static IReadOnlyDictionary<string, string> BuildProjectCharacteristics(Project project)
    {
        var characteristics = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(project.Type))
            characteristics["Тип проекта"] = project.Type;

        if (!string.IsNullOrEmpty(project.Customer))
            characteristics["Заказчик"] = project.Customer;

        if (!string.IsNullOrEmpty(project.Contractor))
            characteristics["Подрядчик"] = project.Contractor;

        if (!string.IsNullOrEmpty(project.ContractNumber))
            characteristics["Номер договора"] = project.ContractNumber;

        if (!string.IsNullOrEmpty(project.WorkStatus))
            characteristics["Статус работ"] = project.WorkStatus;

        if (!string.IsNullOrEmpty(project.Program))
            characteristics["Программа"] = project.Program;

        if (!string.IsNullOrEmpty(project.Gip))
            characteristics["ГИП"] = project.Gip;

        return characteristics;
    }

    public async Task<ProjectForecastAssessmentVolumesDto> GetAssessmentVolumesByDirectionAsync(
        int projectId,
        int versionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var pvList = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Include(pv => pv.PhysicalVolumeKfo).ThenInclude(k => k!.Unit)
            .Include(pv => pv.PhysicalVolumeBer).ThenInclude(b => b!.Unit)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .WherePositiveAssessmentWithClassifier()
            .ToListAsync();

        var pvByDirection = pvList
            .GroupBy(pv => pv.GetClassifierId()!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pv =>
                {
                    var coeff = pv.GetCoefficient();
                    return new ProjectVersionPhysicalVolumeDto(
                        Code: pv.GetVolumeCode(),
                        Name: pv.GetVolumeName(),
                        UnitName: IicWorkDirectionDisplayUnit.ResolveProjectPhysicalVolumeUnitName(pv),
                        Quantity: pv.Quantity,
                        Coefficient: coeff,
                        Total: pv.Quantity * coeff);
                })
                .OrderBy(p => p.Code)
                .ToList());

        var eqList = await context.ProjectEquipments
            .AsNoTracking()
            .Include(eq => eq.Equipment).ThenInclude(e => e!.Unit)
            .Where(eq => eq.ProjectId == projectId
                         && eq.ProjectAnalysisVersionId == versionId
                         && eq.Quantity > 0
                         && eq.Equipment!.IsAssessment == true
                         && eq.Equipment.ClassifierId != null)
            .ToListAsync();

        var eqByDirection = eqList
            .GroupBy(eq => eq.Equipment!.ClassifierId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(eq =>
                {
                    var coeff = eq.Equipment?.Coefficient ?? 1m;
                    return new ProjectVersionPhysicalVolumeDto(
                        Code: eq.CatalogId ?? string.Empty,
                        Name: eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                        UnitName: eq.Equipment?.Unit?.Name,
                        Quantity: eq.Quantity,
                        Coefficient: coeff,
                        Total: eq.QuantityInIicGroupUnits());
                })
                .OrderBy(p => p.Code)
                .ThenBy(p => p.Name)
                .ToList());

        return new ProjectForecastAssessmentVolumesDto(pvByDirection, eqByDirection);
    }
}
