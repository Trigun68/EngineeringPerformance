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

    public ProjectVersionService(AppDbContext context, IProjectVersionDeepCopyService deepCopy)
    {
        _context = context;
        _deepCopy = deepCopy;
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
        var entity = new ProjectAnalysisVersion
        {
            ProjectId = projectId,
            Name = request.Name?.Trim() ?? request.VersionName.Trim(),
            VersionName = request.VersionName.Trim(),
            Description = request.Description,
            Direction = request.Direction,
            PhysicalVolumeTypeId = request.PhysicalVolumeTypeId,
            CreatedAt = DateTime.UtcNow,
        };

        _context.ProjectAnalysisVersions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await AnalysisVersionSnapshotService.PopulateSnapshotAsync(
            _context, entity.Id, entity.CreatedAt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(projectId, entity.Id, cancellationToken))!;
    }

    public async Task<ProjectVersionDto?> UpdateAsync(int projectId, int versionId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null)
            return null;

        entity.Name = request.Name?.Trim() ?? request.VersionName.Trim();
        entity.VersionName = request.VersionName.Trim();
        entity.Description = request.Description;
        entity.Direction = request.Direction;
        entity.PhysicalVolumeTypeId = request.PhysicalVolumeTypeId;
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
            v.IsArchived);
}
