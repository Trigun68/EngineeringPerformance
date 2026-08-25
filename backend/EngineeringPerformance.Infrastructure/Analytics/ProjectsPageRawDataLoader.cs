using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>Сырые данные версии для всех 9 гридов.</summary>
public sealed class ProjectsPageVersionBundle
{
    public required bool VersionExists { get; init; }
    public required IReadOnlyList<IicWorkDirection> WorkDirections { get; init; }
    public required IReadOnlyList<ProjectPhysicalVolume> PhysicalVolumes { get; init; }
    public required IReadOnlyList<ProjectEquipment> Equipment { get; init; }
    public required IReadOnlyList<ProjectLaborIntensity> LaborIntensities { get; init; }
    public required IReadOnlyDictionary<int, NormCalculationSnapshot> NormsByDirection { get; init; }
    public required IReadOnlyDictionary<int, IicWorkDirection> ClassifierMap { get; init; }
    public required IReadOnlyDictionary<int, PlanningZParameters> PlanningZByDirection { get; init; }
    public required IReadOnlyDictionary<int, int> NormIdByDirection { get; init; }
    public PlanningScenario PlanningScenario { get; init; } = PlanningScenario.ByEachValue;
    public AnalysisDirection Direction { get; init; } = AnalysisDirection.Fact;
}

/// <summary>
/// Три логических батча SQL на версию (EF Core не допускает параллель на одном DbContext):
/// 1) справочник IIC + проверка версии; 2) PPV + Equipment; 3) LI + снимки норм.
/// Соединение берётся из пула EP-Analytics и отпускается до CPU-сборки гридов.
/// </summary>
public sealed class ProjectsPageRawDataLoader : IProjectsPageRawDataLoader
{
    private readonly IAnalyticsDbContextFactory _analyticsDb;
    private readonly AnalyticsSqlGate _sqlGate;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private List<IicWorkDirection>? _workDirectionsCache;

    public ProjectsPageRawDataLoader(
        IAnalyticsDbContextFactory analyticsDb,
        AnalyticsSqlGate sqlGate,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility)
    {
        _analyticsDb = analyticsDb;
        _sqlGate = sqlGate;
        _currentUser = currentUser;
        _visibility = visibility;
    }

    public async Task<ProjectsPageVersionBundle> LoadAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Read, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot read project {projectId}.");

        await _sqlGate.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _analyticsDb.CreateDbContextAsync(cancellationToken);
            return await LoadWithContextAsync(context, projectId, versionId, cancellationToken);
        }
        finally
        {
            _sqlGate.Release();
        }
    }

    public async Task<IReadOnlyDictionary<int, ProjectsPageVersionBundle>> LoadManyAsync(
        IReadOnlyList<(int ProjectId, int VersionId)> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return new Dictionary<int, ProjectsPageVersionBundle>();

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToHashSet();

        // VersionId глобально уникален (PK), поэтому фильтр по нему достаточен.
        var allowedKeys = keys
            .Where(k => accessible.Contains(k.ProjectId))
            .GroupBy(k => k.VersionId)
            .Select(g => g.First())
            .ToList();
        if (allowedKeys.Count == 0)
            return new Dictionary<int, ProjectsPageVersionBundle>();

        var versionIds = allowedKeys.Select(k => k.VersionId).ToList();

        await _sqlGate.WaitAsync(cancellationToken);
        try
        {
            await using var context = await _analyticsDb.CreateDbContextAsync(cancellationToken);

            var workDirections = await GetWorkDirectionsAsync(context, cancellationToken);
            var classifierMap = workDirections.ToDictionary(d => d.Id);

            var versionRows = await context.ProjectAnalysisVersions
                .AsNoTracking()
                .Where(v => versionIds.Contains(v.Id))
                .Select(v => new { v.Id, v.ProjectId, v.PlanningScenario, v.Direction })
                .ToListAsync(cancellationToken);
            var versionById = versionRows.ToDictionary(v => v.Id);

            var physicalVolumes = await context.ProjectPhysicalVolumes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
                .Where(pv => versionIds.Contains(pv.ProjectAnalysisVersionId))
                .ToListAsync(cancellationToken);

            var equipment = await context.ProjectEquipments
                .AsNoTracking()
                .Include(e => e.Equipment)
                .Where(e => versionIds.Contains(e.ProjectAnalysisVersionId))
                .ToListAsync(cancellationToken);

            var laborIntensities = await context.ProjectLaborIntensities
                .AsNoTracking()
                .Where(li => versionIds.Contains(li.ProjectAnalysisVersionId))
                .ToListAsync(cancellationToken);

            var snapshots = await context.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => versionIds.Contains(s.ProjectAnalysisVersionId))
                .Include(s => s.Norm)
                .ToListAsync(cancellationToken);

            var planningRows = await context.ProjectVersionPlanningParameters
                .AsNoTracking()
                .Where(p => versionIds.Contains(p.ProjectAnalysisVersionId))
                .Select(p => new { p.ProjectAnalysisVersionId, p.IicWorkDirectionId, p.ZPessimistic, p.ZBase, p.ZOptimistic })
                .ToListAsync(cancellationToken);

            var pvByVersion = physicalVolumes.GroupBy(pv => pv.ProjectAnalysisVersionId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ProjectPhysicalVolume>)g.ToList());
            var eqByVersion = equipment.GroupBy(e => e.ProjectAnalysisVersionId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ProjectEquipment>)g.ToList());
            var liByVersion = laborIntensities.GroupBy(li => li.ProjectAnalysisVersionId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ProjectLaborIntensity>)g.ToList());
            var snapByVersion = snapshots.GroupBy(s => s.ProjectAnalysisVersionId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var planningByVersion = planningRows.GroupBy(p => p.ProjectAnalysisVersionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<int, ProjectsPageVersionBundle>();
            foreach (var key in allowedKeys)
            {
                if (!versionById.TryGetValue(key.VersionId, out var version))
                    continue; // версия удалена/архивирована между запросами — пропускаем

                var versionSnapshots = snapByVersion.GetValueOrDefault(key.VersionId) ?? [];
                var normsByDirection = versionSnapshots
                    .Where(s => s.Norm is not null)
                    .GroupBy(s => s.WorkDirectionId)
                    .ToDictionary(
                        g => g.Key,
                        g => NormSnapshotFactory.FromEntity(g.First().Norm!));

                result[key.VersionId] = new ProjectsPageVersionBundle
                {
                    VersionExists = true,
                    WorkDirections = workDirections,
                    PhysicalVolumes = pvByVersion.GetValueOrDefault(key.VersionId) ?? [],
                    Equipment = eqByVersion.GetValueOrDefault(key.VersionId) ?? [],
                    LaborIntensities = liByVersion.GetValueOrDefault(key.VersionId) ?? [],
                    NormsByDirection = normsByDirection,
                    ClassifierMap = classifierMap,
                    PlanningZByDirection = planningByVersion.GetValueOrDefault(key.VersionId)?
                        .ToDictionary(
                            p => p.IicWorkDirectionId,
                            p => new PlanningZParameters(p.ZPessimistic, p.ZBase, p.ZOptimistic))
                        ?? new Dictionary<int, PlanningZParameters>(),
                    NormIdByDirection = versionSnapshots
                        .GroupBy(s => s.WorkDirectionId)
                        .ToDictionary(g => g.Key, g => g.First().NormId),
                    PlanningScenario = version.PlanningScenario,
                    Direction = version.Direction,
                };
            }

            return result;
        }
        finally
        {
            _sqlGate.Release();
        }
    }

    private async Task<ProjectsPageVersionBundle> LoadWithContextAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var workDirections = await GetWorkDirectionsAsync(context, cancellationToken);
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.Id == versionId && v.ProjectId == projectId)
            .Select(v => new { v.Id, v.PlanningScenario, v.Direction })
            .FirstOrDefaultAsync(cancellationToken);
        var versionExists = version is not null;

        var physicalVolumes = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var equipment = await context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var laborIntensities = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection;
        IReadOnlyDictionary<int, int> normIdByDirection;
        if (versionExists)
        {
            var snapshots = await context.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Include(s => s.Norm)
                .ToListAsync(cancellationToken);

            normsByDirection = snapshots
                .Where(s => s.Norm is not null)
                .GroupBy(s => s.WorkDirectionId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var norm = g.First().Norm!;
                        return NormSnapshotFactory.FromEntity(norm);
                    });

            normIdByDirection = snapshots
                .GroupBy(s => s.WorkDirectionId)
                .ToDictionary(g => g.Key, g => g.First().NormId);
        }
        else
        {
            normsByDirection = new Dictionary<int, NormCalculationSnapshot>();
            normIdByDirection = new Dictionary<int, int>();
        }

        var planningRows = await context.ProjectVersionPlanningParameters
            .AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .Select(p => new { p.IicWorkDirectionId, p.ZPessimistic, p.ZBase, p.ZOptimistic })
            .ToListAsync(cancellationToken);

        var planningZByDirection = planningRows.ToDictionary(
            p => p.IicWorkDirectionId,
            p => new PlanningZParameters(p.ZPessimistic, p.ZBase, p.ZOptimistic));

        return new ProjectsPageVersionBundle
        {
            VersionExists = versionExists,
            WorkDirections = workDirections,
            PhysicalVolumes = physicalVolumes,
            Equipment = equipment,
            LaborIntensities = laborIntensities,
            NormsByDirection = normsByDirection,
            ClassifierMap = workDirections.ToDictionary(d => d.Id),
            PlanningZByDirection = planningZByDirection,
            NormIdByDirection = normIdByDirection,
            PlanningScenario = version?.PlanningScenario ?? PlanningScenario.ByEachValue,
            Direction = version?.Direction ?? AnalysisDirection.Fact,
        };
    }

    private async Task<List<IicWorkDirection>> GetWorkDirectionsAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (_workDirectionsCache is not null)
            return _workDirectionsCache;

        _workDirectionsCache = await context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);

        return _workDirectionsCache;
    }
}
