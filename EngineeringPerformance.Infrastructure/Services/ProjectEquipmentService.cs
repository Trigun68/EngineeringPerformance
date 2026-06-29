using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectEquipmentService : IDataTableService<ProjectEquipment>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectEquipmentService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ProjectEquipment>> GetByProjectVersionAsync(int projectAnalysisVersionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectAnalysisVersionId == projectAnalysisVersionId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProjectEquipment>> GetAllForExplorerAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.ProjectAnalysisVersion)
            .Include(e => e.Equipment)
            .OrderBy(e => e.ProjectId)
            .ThenBy(e => e.ProjectAnalysisVersionId)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectEquipment?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ProjectEquipment> CreateAsync(ProjectEquipment item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ProjectEquipments.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(ProjectEquipment item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        item.ModifiedAt = DateTime.UtcNow;
        context.ProjectEquipments.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.ProjectEquipments.FindAsync(id);
        if (entity is not null)
        {
            context.ProjectEquipments.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
