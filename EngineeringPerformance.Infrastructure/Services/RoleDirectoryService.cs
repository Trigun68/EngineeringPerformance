using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class RoleDirectoryService : DirectoryServiceBase<Role>
{
    public RoleDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<Role> GetDbSet(AppDbContext context) => context.Roles;
}
