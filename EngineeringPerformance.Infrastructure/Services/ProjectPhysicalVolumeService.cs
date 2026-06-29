using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectPhysicalVolumeService : IDataTableService<ProjectPhysicalVolume>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectPhysicalVolumeService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static IQueryable<ProjectPhysicalVolume> WithCatalogIncludes(IQueryable<ProjectPhysicalVolume> query) =>
        query.Include(pv => pv.PhysicalVolumeKfo).ThenInclude(k => k!.Unit)
             .Include(pv => pv.PhysicalVolumeBer).ThenInclude(b => b!.Unit);

    public async Task<IReadOnlyList<ProjectPhysicalVolume>> GetByProjectVersionAsync(int projectAnalysisVersionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await WithCatalogIncludes(context.ProjectPhysicalVolumes.AsNoTracking())
            .Where(pv => pv.ProjectAnalysisVersionId == projectAnalysisVersionId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProjectPhysicalVolume>> GetAllForExplorerAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await WithCatalogIncludes(context.ProjectPhysicalVolumes.AsNoTracking())
            .Include(pv => pv.Project)
            .Include(pv => pv.ProjectAnalysisVersion)
            .OrderBy(pv => pv.ProjectId)
            .ThenBy(pv => pv.ProjectAnalysisVersionId)
            .ThenBy(pv => pv.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectPhysicalVolume?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await WithCatalogIncludes(context.ProjectPhysicalVolumes.AsNoTracking())
            .FirstOrDefaultAsync(pv => pv.Id == id);
    }

    public async Task<ProjectPhysicalVolume> CreateAsync(ProjectPhysicalVolume item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ProjectPhysicalVolumes.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(ProjectPhysicalVolume item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        item.ModifiedAt = DateTime.UtcNow;
        context.ProjectPhysicalVolumes.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.ProjectPhysicalVolumes.FindAsync(id);
        if (entity is not null)
        {
            context.ProjectPhysicalVolumes.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
