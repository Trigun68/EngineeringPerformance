using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class UnitOfMeasurementDirectoryService : DirectoryServiceBase<UnitOfMeasurement>
{
    public UnitOfMeasurementDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<UnitOfMeasurement> GetDbSet(AppDbContext context) => context.UnitsOfMeasurement;
}