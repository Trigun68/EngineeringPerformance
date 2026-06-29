using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

public sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }
}
