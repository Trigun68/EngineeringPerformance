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

/// <summary>Сырые данные версии для всех 9 гридов (один scoped DbContext).</summary>
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
}

/// <summary>
/// Три логических батча SQL на версию (EF Core не допускает параллель на одном DbContext):
/// 1) справочник IIC + проверка версии; 2) PPV + Equipment; 3) LI + снимки норм.
/// </summary>
public sealed class ProjectsPageRawDataLoader
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private List<IicWorkDirection>? _workDirectionsCache;

    public ProjectsPageRawDataLoader(
        AppDbContext context,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility)
    {
        _context = context;
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

        // Batch 1: IIC + version
        var workDirections = await GetWorkDirectionsAsync(cancellationToken);
        var version = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.Id == versionId && v.ProjectId == projectId)
            .Select(v => new { v.Id, v.PlanningScenario })
            .FirstOrDefaultAsync(cancellationToken);
        var versionExists = version is not null;

        // Batch 2: объёмы и оборудование
        var physicalVolumes = await _context.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var equipment = await _context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        // Batch 3: трудоёмкости + снимки норм
        var laborIntensities = await _context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection;
        IReadOnlyDictionary<int, int> normIdByDirection;
        if (versionExists)
        {
            var snapshots = await _context.AnalysisVersionSnapshots
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

        var planningRows = await _context.ProjectVersionPlanningParameters
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
        };
    }

    private async Task<List<IicWorkDirection>> GetWorkDirectionsAsync(CancellationToken cancellationToken)
    {
        if (_workDirectionsCache is not null)
            return _workDirectionsCache;

        _workDirectionsCache = await _context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);

        return _workDirectionsCache;
    }
}
