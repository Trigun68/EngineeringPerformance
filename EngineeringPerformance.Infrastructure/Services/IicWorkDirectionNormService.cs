using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class IicWorkDirectionNormService
    : IDateFilteredRegisterService<IicWorkDirectionNorm>, IIicWorkDirectionNormQueryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public IicWorkDirectionNormService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<IicWorkDirectionNorm>> GetAllAsync()
    {
        return await GetByDateAsync(DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<IicWorkDirectionNorm>> GetByDateAsync(DateTime date)
    {
        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .Where(n => n.EffectiveFrom <= utcDate && (n.EffectiveTo == null || n.EffectiveTo >= utcDate))
            .ToListAsync();
    }

    public async Task<IicWorkDirectionNorm?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IicWorkDirectionNorm> CreateAsync(IicWorkDirectionNorm item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.IicWorkDirectionNorms.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(IicWorkDirectionNorm item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        item.ModifiedAt = DateTime.UtcNow;
        context.IicWorkDirectionNorms.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.IicWorkDirectionNorms.FindAsync(id);
        if (entity is not null)
        {
            context.IicWorkDirectionNorms.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<IicWorkDirectionNorm>> GetHistoryByWorkDirectionIdAsync(int workDirectionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.WorkDirectionId == workDirectionId)
            .OrderByDescending(n => n.EffectiveFrom)
            .ToListAsync();
    }
}
