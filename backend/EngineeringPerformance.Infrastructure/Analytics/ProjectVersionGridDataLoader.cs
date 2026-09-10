using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class ProjectVersionGridLoadOptions
{
    public bool IncludeLaborIntensities { get; init; }
}

public sealed class ProjectLaborIntensityLoadRow
{
    public int? LaborIntensityId { get; init; }
    public string? DocumentationCode { get; init; }
    public int? WorkDirectionId { get; set; }
    public decimal PlannedHours { get; init; }
}

public sealed class ProjectVersionGridLoadResult
{
    public required IReadOnlyDictionary<int, IicWorkDirection> ClassifierMap { get; init; }
    public required IReadOnlyList<ProjectPhysicalVolume> AllPhysicalVolumes { get; init; }
    public required IReadOnlyList<ProjectEquipment> AllEquipment { get; init; }
    public required IReadOnlyDictionary<int, int> SnapshotNormIdByDirection { get; init; }
    public IReadOnlyList<ProjectLaborIntensityLoadRow> LaborIntensities { get; init; } = [];
}

/// <summary>
/// Загрузка сырых данных версии для расчёта факта (оценка / сравнение).
/// </summary>
public sealed class ProjectVersionGridDataLoader
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;

    public ProjectVersionGridDataLoader(
        AppDbContext context,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility)
    {
        _context = context;
        _currentUser = currentUser;
        _visibility = visibility;
    }

    public async Task<ProjectVersionGridLoadResult> LoadAsync(
        int projectId,
        int versionId,
        ProjectVersionGridLoadOptions options,
        CancellationToken cancellationToken = default)
    {
        // GAP WPF: ProjectVisibility + Level.Read перед загрузкой гридов.
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Read, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot read project {projectId}.");

        // GAP: один scoped DbContext на запрос (все чтения — AsNoTracking для UI).
        var classifierMap = await _context.IicWorkDirections
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var allPhysicalVolumes = await _context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var allEquipment = await _context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var snapshotNormIdByDirection = await _context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .ToDictionaryAsync(s => s.WorkDirectionId, s => s.NormId, cancellationToken);

        IReadOnlyList<ProjectLaborIntensityLoadRow> laborIntensities = [];
        if (options.IncludeLaborIntensities)
        {
            var createdAt = await _context.ProjectAnalysisVersions.AsNoTracking()
                .Where(v => v.Id == versionId && v.ProjectId == projectId)
                .Select(v => v.CreatedAt)
                .FirstAsync(cancellationToken);

            var rows = await _context.ProjectLaborIntensities
                .AsNoTracking()
                .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
                .Select(li => new ProjectLaborIntensityLoadRow
                {
                    LaborIntensityId = li.LaborIntensityId,
                    DocumentationCode = li.DocumentationCode,
                    WorkDirectionId = li.WorkDirectionId,
                    PlannedHours = li.PlannedHours,
                })
                .ToListAsync(cancellationToken);

            var slice = await LaborIntensityIicAsOf.LoadAsync(
                _context,
                createdAt,
                rows.Select(r => r.LaborIntensityId ?? 0),
                cancellationToken);
            foreach (var row in rows)
            {
                row.WorkDirectionId = LaborIntensityIicAsOf.ResolveDirection(
                    slice, row.LaborIntensityId, row.WorkDirectionId);
            }

            laborIntensities = rows;
        }

        return new ProjectVersionGridLoadResult
        {
            ClassifierMap = classifierMap,
            AllPhysicalVolumes = allPhysicalVolumes,
            AllEquipment = allEquipment,
            SnapshotNormIdByDirection = snapshotNormIdByDirection,
            LaborIntensities = laborIntensities,
        };
    }
}
