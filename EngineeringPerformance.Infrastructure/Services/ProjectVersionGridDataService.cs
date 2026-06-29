using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectVersionGridLoadOptions
{
    public bool IncludeUnitsOnDirections { get; init; }
    public bool IncludeLaborIntensities { get; init; }
    public bool IncludeActualHoursInLabor { get; init; }
}

public sealed class ProjectLaborIntensityLoadRow
{
    public string? DocumentationCode { get; init; }
    public int WorkDirectionId { get; init; }
    public decimal PlannedHours { get; init; }
    public decimal? ActualHours { get; init; }
}

public sealed class ProjectVersionGridLoadResult
{
    public required IReadOnlyDictionary<int, IicWorkDirection> ClassifierMap { get; init; }
    public required IReadOnlyList<ProjectPhysicalVolume> AssessmentPhysicalVolumes { get; init; }
    public required IReadOnlyList<ProjectPhysicalVolume> AllPhysicalVolumes { get; init; }
    public required IReadOnlyList<ProjectEquipment> AllEquipment { get; init; }
    public required IReadOnlyDictionary<int, int> SnapshotNormIdByDirection { get; init; }
    public IReadOnlyList<ProjectLaborIntensityLoadRow> LaborIntensities { get; init; } = [];
}

public sealed class ProjectVersionGridDataService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectVersionGridDataService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ProjectVersionGridLoadResult> LoadAsync(
        int projectId,
        int versionId,
        ProjectVersionGridLoadOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var directionsQuery = ctx.IicWorkDirections.AsNoTracking();
        if (options.IncludeUnitsOnDirections)
            directionsQuery = directionsQuery.Include(d => d.Unit);

        var classifierMap = await directionsQuery.ToDictionaryAsync(c => c.Id, cancellationToken);

        static IQueryable<ProjectPhysicalVolume> WithCatalogIncludes(IQueryable<ProjectPhysicalVolume> q) =>
            q.Include(pv => pv.PhysicalVolumeKfo).ThenInclude(k => k!.Unit)
             .Include(pv => pv.PhysicalVolumeBer).ThenInclude(b => b!.Unit);

        var assessmentPhysicalVolumes = await WithCatalogIncludes(ctx.ProjectPhysicalVolumes.AsNoTracking())
            .Where(pv => pv.ProjectId == projectId
                         && pv.ProjectAnalysisVersionId == versionId
                         && ((pv.PhysicalVolumeKfo != null && pv.PhysicalVolumeKfo.IsAssessment == true && pv.PhysicalVolumeKfo.ClassifierId != null)
                             || (pv.PhysicalVolumeBer != null && pv.PhysicalVolumeBer.IsAssessment == true && pv.PhysicalVolumeBer.ClassifierId != null)))
            .ToListAsync(cancellationToken);

        var allPhysicalVolumes = await WithCatalogIncludes(ctx.ProjectPhysicalVolumes.AsNoTracking())
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var allEquipment = await ctx.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var snapshotNormIdByDirection = await ctx.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .ToDictionaryAsync(s => s.WorkDirectionId, s => s.NormId, cancellationToken);

        IReadOnlyList<ProjectLaborIntensityLoadRow> laborIntensities = [];
        if (options.IncludeLaborIntensities)
        {
            if (options.IncludeActualHoursInLabor)
            {
                laborIntensities = await ctx.ProjectLaborIntensities
                    .AsNoTracking()
                    .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
                    .Select(li => new ProjectLaborIntensityLoadRow
                    {
                        DocumentationCode = li.DocumentationCode,
                        WorkDirectionId = li.WorkDirectionId,
                        PlannedHours = li.PlannedHours,
                        ActualHours = li.ActualHours,
                    })
                    .ToListAsync(cancellationToken);
            }
            else
            {
                laborIntensities = await ctx.ProjectLaborIntensities
                    .AsNoTracking()
                    .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
                    .Select(li => new ProjectLaborIntensityLoadRow
                    {
                        DocumentationCode = li.DocumentationCode,
                        WorkDirectionId = li.WorkDirectionId,
                        PlannedHours = li.PlannedHours,
                    })
                    .ToListAsync(cancellationToken);
            }
        }

        return new ProjectVersionGridLoadResult
        {
            ClassifierMap = classifierMap,
            AssessmentPhysicalVolumes = assessmentPhysicalVolumes,
            AllPhysicalVolumes = allPhysicalVolumes,
            AllEquipment = allEquipment,
            SnapshotNormIdByDirection = snapshotNormIdByDirection,
            LaborIntensities = laborIntensities,
        };
    }

    public async Task<IReadOnlyList<IicDirectionNodeDto>> GetActiveDirectionNodesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new IicDirectionNodeDto(
                d.Id,
                d.ParentId,
                d.Name,
                d.Code,
                d.SortOrder,
                d.Unit != null ? d.Unit.Name : null))
            .ToListAsync(cancellationToken);
    }
}
