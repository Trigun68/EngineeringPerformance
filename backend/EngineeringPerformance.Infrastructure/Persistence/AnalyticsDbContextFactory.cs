using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Persistence;

public sealed class AnalyticsDbContextFactory(DbContextOptions<AppDbContext> options) : IAnalyticsDbContextFactory
{
    public AppDbContext CreateDbContext() => new(options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }
}

/// <summary>Тесты: тот же factory, что и scoped interactive (одна in-memory / test SQL).</summary>
public sealed class DelegatingAnalyticsDbContextFactory(IDbContextFactory<AppDbContext> inner)
    : IAnalyticsDbContextFactory
{
    public AppDbContext CreateDbContext() => inner.CreateDbContext();

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        inner.CreateDbContextAsync(cancellationToken);
}
