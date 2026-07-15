using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectVersionService : IProjectVersionService
{
    private readonly AppDbContext _context;
    private readonly IProjectVersionDeepCopyService _deepCopy;
    private readonly IProjectVersionPlanningParameterService _planningParameters;

    public ProjectVersionService(
        AppDbContext context,
        IProjectVersionDeepCopyService deepCopy,
        IProjectVersionPlanningParameterService planningParameters)
    {
        _context = context;
        _deepCopy = deepCopy;
        _planningParameters = planningParameters;
    }

    public async Task<IReadOnlyList<ProjectVersionDto>> ListAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived)
            .OrderBy(v => v.SortOrder)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectVersionDto?> GetByIdAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && v.Id == versionId)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectVersionDto> CreateAsync(int projectId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePhysicalVolumeTypeAsync(request.PhysicalVolumeTypeId, cancellationToken);

        var entity = new ProjectAnalysisVersion
        {
            ProjectId = projectId,
            Name = request.Name?.Trim() ?? request.VersionName.Trim(),
            VersionName = request.VersionName.Trim(),
            Description = request.Description,
            Direction = request.Direction,
            PhysicalVolumeTypeId = request.PhysicalVolumeTypeId,
            PlanningScenario = request.PlanningScenario ?? Domain.Norms.PlanningScenario.ByEachValue,
            CreatedAt = DateTime.UtcNow,
        };

        _context.ProjectAnalysisVersions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await AnalysisVersionSnapshotService.PopulateSnapshotAsync(
            _context, entity.Id, entity.CreatedAt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _planningParameters.SeedForVersionAsync(entity.Id, cancellationToken);

        return (await GetByIdAsync(projectId, entity.Id, cancellationToken))!;
    }

    public async Task<ProjectVersionDto?> UpdateAsync(int projectId, int versionId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null)
            return null;

        await ValidatePhysicalVolumeTypeAsync(request.PhysicalVolumeTypeId, cancellationToken);

        entity.Name = request.Name?.Trim() ?? request.VersionName.Trim();
        entity.VersionName = request.VersionName.Trim();
        entity.Description = request.Description;
        entity.Direction = request.Direction;
        entity.PhysicalVolumeTypeId = request.PhysicalVolumeTypeId;
        if (request.PlanningScenario.HasValue)
            entity.PlanningScenario = request.PlanningScenario.Value;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(projectId, versionId, cancellationToken);
    }

    public async Task<bool> ArchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null)
            return false;

        entity.IsArchived = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ProjectVersionDto> CopyAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        CancellationToken cancellationToken = default) =>
        _deepCopy.CopyVersionAsync(projectId, versionId, newVersionName, newDescription, cancellationToken);

    private async Task ValidatePhysicalVolumeTypeAsync(int? physicalVolumeTypeId, CancellationToken cancellationToken)
    {
        if (physicalVolumeTypeId is not int typeId)
            throw new InvalidOperationException(
                "Не выбран справочник физобъёмов. Укажите КФО или БЭР для версии проекта.");

        var typeName = await _context.PhysicalVolumeTypes.AsNoTracking()
            .Where(t => t.Id == typeId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(typeName)
            || (!string.Equals(typeName, PhysicalVolumeCatalogBinding.KfoTypeName, StringComparison.Ordinal)
                && !string.Equals(typeName, PhysicalVolumeCatalogBinding.BerTypeName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Справочник физобъёмов должен быть КФО или БЭР.");
        }
    }

    private static System.Linq.Expressions.Expression<Func<ProjectAnalysisVersion, ProjectVersionDto>> Map =>
        v => new ProjectVersionDto(
            v.Id,
            v.ProjectId,
            v.Name,
            v.VersionName,
            v.Description,
            v.Direction,
            v.PhysicalVolumeTypeId,
            v.SortOrder,
            v.IsArchived,
            v.CreatedAt,
            v.PlanningScenario);
}
