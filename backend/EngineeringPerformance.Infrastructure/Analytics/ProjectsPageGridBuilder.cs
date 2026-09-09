using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>CPU-сборка 9 гридов из уже загруженного bundle (без доп. SQL).</summary>
public static partial class ProjectsPageGridBuilder
{
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeLivePpvHours(
        ProjectsPageVersionBundle bundle) =>
        StoredPlanHours.ByPpvId(bundle);

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        ProjectsPageVersionBundle bundle) =>
        StoredPlanHours.ByPpvId(bundle);

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        ProjectsPageVersionBundle bundle) =>
        StoredPlanHours.ByDirection(bundle);

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

    private static decimal? NullIfNonPositive(decimal value) =>
        value > 0m ? Math.Round(value, 2) : null;

    /// <summary>
    /// Плановые часы для UI: сохранённые LaborIntensity* ФО и оборудования.
    /// </summary>
    public static IReadOnlyList<PerformanceRowDto> AttachLivePlanHours(
        IReadOnlyList<PerformanceRowDto> performance,
        ProjectsPageVersionBundle bundle)
    {
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

    public static IReadOnlyList<PerformanceRowDto> BuildPerformance(
        ProjectsPageVersionBundle bundle,
        IPerformanceCalculationEngine engine) =>
        BuildPerformanceFromLabor(
            bundle,
            engine,
            bundle.LaborIntensities
                .Select(li => new LaborIntensityCalculationLine(li.WorkDirectionId, li.PlannedHours))
                .ToList(),
            rollHoursToFirstLevel: true);

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
        return BuildPerformanceFromLabor(bundle, engine, labor, rollHoursToFirstLevel: false);
    }

    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeLivePlanHoursByDirection(
        ProjectsPageVersionBundle bundle) =>
        AggregateForecastHoursByDirection(bundle);

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
        IReadOnlyList<LaborIntensityCalculationLine> labor,
        bool rollHoursToFirstLevel)
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
            NormsByDirection: bundle.NormsByDirection,
            RollHoursToFirstLevel: rollHoursToFirstLevel,
            ClaimedWorkDirectionIds: rollHoursToFirstLevel ? bundle.ClaimedWorkDirectionIds : null);

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

    /// <summary>
    /// Сохранённые часы по направлениям (ФО + оборудование). Без live N(V).
    /// </summary>
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ResolveLivePlanHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        _ = performance;
        return ComputeLivePlanHoursByDirection(bundle);
    }

    private static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ResolveScenarioHoursByDirection(
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance)
    {
        _ = performance;
        return ComputeLivePlanHoursByDirection(bundle);
    }
}
