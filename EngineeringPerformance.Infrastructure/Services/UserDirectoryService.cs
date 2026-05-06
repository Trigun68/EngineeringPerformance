using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class UserDirectoryService : DirectoryServiceBase<User>
{
    public UserDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<User> GetDbSet(AppDbContext context) => context.Users;

    public override async Task<IReadOnlyList<User>> GetAllAsync(bool includeArchived = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        IQueryable<User> query = GetDbSet(context)
            .AsNoTracking()
            .Include(u => u.Role);

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        return await query.ToListAsync();
    }

    public override async Task<User?> GetByIdAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await GetDbSet(context)
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
