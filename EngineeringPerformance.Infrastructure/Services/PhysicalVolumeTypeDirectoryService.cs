using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class PhysicalVolumeTypeDirectoryService : DirectoryServiceBase<PhysicalVolumeType>
{
    public PhysicalVolumeTypeDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<PhysicalVolumeType> GetDbSet(AppDbContext context) => context.PhysicalVolumeTypes;
}
