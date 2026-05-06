using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

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
        IReadOnlyDictionary<int, decimal>? nodePlannedTotalsByWorkDirectionId = null)
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

        var laborIntensities = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.WorkDirection)
            .ThenInclude(d => d!.Unit)
            .Include(li => li.PhysicalVolume)
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var physicalVolumes = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Include(pv => pv.PhysicalVolume)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
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

        var normByDirection = snapshotNorms.ToDictionary(n => n.WorkDirectionId);

        var volumeByDirection = physicalVolumes
            .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true && pv.PhysicalVolume.IsAssessment == true)
            .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));

        if (nodePlannedTotalsByWorkDirectionId is not null)
        {
            var direct = ComputeDirectVolumesFromNodeTotals(workDirections, nodePlannedTotalsByWorkDirectionId);
            foreach (var kv in direct)
            {
                volumeByDirection[kv.Key] = kv.Value;
            }
        }

        var hoursByDirection = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.PlannedHours));

        var plannedTree = BuildIicPlannedVolumeTree(
            workDirections,
            normByDirection,
            volumeByDirection,
            hoursByDirection);

        var scenarioFlatRows = BuildScenarioDisciplineRows(plannedTree, workDirections);

        var scenarios = BuildScenarios(scenarioFlatRows);

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
        IReadOnlyDictionary<int, decimal> hoursByDirection)
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

        void Walk(IicWorkDirection d, string pathPrefix)
        {
            if (!walkedIds.Add(d.Id))
            {
                return;
            }

            var path = string.IsNullOrEmpty(pathPrefix) ? d.Name : $"{pathPrefix} / {d.Name}";
            normByDirection.TryGetValue(d.Id, out var norm);
            var unitName = IicWorkDirectionDisplayUnit.ResolveUnitName(d, byId);

            var vol = totalVolume.GetValueOrDefault(d.Id);
            var laborHours = hoursByDirection.GetValueOrDefault(d.Id);

            var baseHours = ComputeBaseHours(vol, norm, laborHours);

            var (opt, pes, range) = ComputeScenarioHours(baseHours, norm, vol);

            var isLeaf = !hasChildren.Contains(d.Id);
            if (!isLeaf)
            {
                baseHours = 0m;
                opt = 0m;
                pes = 0m;
                range = "—";
                var childSumHours = childrenByParent.GetValueOrDefault(d.Id)?
                    .Sum(c => GetLeafHours(c.Id)) ?? 0m;
                if (childSumHours > 0)
                {
                    baseHours = Math.Round(childSumHours, 2);
                    (opt, pes, range) = ComputeScenarioHours(baseHours, norm, vol);
                }
            }

            results.Add(new ForecastIicVolumeRowDto(
                WorkDirectionId: d.Id,
                ParentId: d.ParentId,
                Name: d.Name,
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

        decimal GetLeafHours(int id)
        {
            if (!leafHoursVisitedIds.Add(id))
            {
                return 0m;
            }

            if (!byId.TryGetValue(id, out var dir))
            {
                return 0m;
            }

            if (!hasChildren.Contains(id))
            {
                normByDirection.TryGetValue(id, out var n);
                var v = totalVolume.GetValueOrDefault(id);
                var lh = hoursByDirection.GetValueOrDefault(id);
                var bh = ComputeBaseHours(v, n, lh);
                return bh;
            }

            return childrenByParent.GetValueOrDefault(id)?.Sum(c => GetLeafHours(c.Id)) ?? 0m;
        }

        foreach (var root in workDirections.Where(d => d.ParentId == null).OrderBy(d => d.SortOrder).ThenBy(d => d.Name))
        {
            Walk(root, string.Empty);
        }

        return results;

        static decimal ComputeBaseHours(decimal plannedVolume, IicWorkDirectionNorm? norm, decimal laborHours)
        {
            // Базовый: ФактФО × НормПроизводительность (20%)
            if (plannedVolume > 0 && norm?.Performance is { } p && p > 0)
            {
                return Math.Round(plannedVolume * p, 2);
            }

            return laborHours;
        }

        static (decimal Opt, decimal Pes, string Range) ComputeScenarioHours(decimal baseHours, IicWorkDirectionNorm? norm, decimal plannedVolume)
        {
            if (baseHours <= 0 || plannedVolume <= 0)
            {
                return (0m, 0m, "—");
            }

            // Оптимистичный (15%): ФО × (Performance − 0.5 × StdDev)
            // Пессимистичный (15%): ФО × (Performance + 0.5 × StdDev)
            if (norm?.Performance is { } p && p > 0 && norm.StandardDeviation is { } std && std > 0)
            {
                var opt = Math.Round(plannedVolume * Math.Max(0m, p - 0.5m * std), 2);
                var pes = Math.Round(plannedVolume * (p + 0.5m * std), 2);
                return (opt, pes, $"{opt:F2} - {pes:F2}");
            }

            return (baseHours, baseHours, $"{baseHours:F2} - {baseHours:F2}");
        }
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

    private static IReadOnlyList<ForecastScenarioDto> BuildScenarios(IReadOnlyList<ForecastIicVolumeRowDto> disciplines)
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
                ZScore: -0.5m,
                TotalLaborHours: optimisticTotal,
                Disciplines: disciplines.Select(d => MapRow(
                    d,
                    d.OptimisticHours,
                    d.BaseHours,
                    d.PessimisticHours,
                    d.Range)).ToList()),
            new(
                ScenarioName: "Базовый",
                ZScore: 0m,
                TotalLaborHours: baseTotal,
                Disciplines: disciplines.Select(d => MapRow(
                    d,
                    d.OptimisticHours,
                    d.BaseHours,
                    d.PessimisticHours,
                    d.Range)).ToList()),
            new(
                ScenarioName: "Пессимистичный",
                ZScore: 0.5m,
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
}
