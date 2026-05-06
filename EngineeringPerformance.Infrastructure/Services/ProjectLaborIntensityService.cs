using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectLaborIntensityService : IDataTableService<ProjectLaborIntensity>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectLaborIntensityService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ProjectLaborIntensity>> GetByProjectVersionAsync(int projectAnalysisVersionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.WorkDirection)
            .Where(li => li.ProjectAnalysisVersionId == projectAnalysisVersionId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProjectLaborIntensity>> GetAllForExplorerAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.Project)
            .Include(li => li.ProjectAnalysisVersion)
            .Include(li => li.WorkDirection)
            .Include(li => li.PhysicalVolume)
            .OrderBy(li => li.ProjectId)
            .ThenBy(li => li.ProjectAnalysisVersionId)
            .ThenBy(li => li.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectLaborIntensity?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.WorkDirection)
            .FirstOrDefaultAsync(li => li.Id == id);
    }

    public async Task<ProjectLaborIntensity> CreateAsync(ProjectLaborIntensity item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ProjectLaborIntensities.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(ProjectLaborIntensity item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        item.ModifiedAt = DateTime.UtcNow;
        context.ProjectLaborIntensities.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.ProjectLaborIntensities.FindAsync(id);
        if (entity is not null)
        {
            context.ProjectLaborIntensities.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
