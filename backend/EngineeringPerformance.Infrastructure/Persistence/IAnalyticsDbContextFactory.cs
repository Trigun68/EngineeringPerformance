namespace EngineeringPerformance.Infrastructure.Persistence;

/// <summary>
/// Фабрика <see cref="AppDbContext"/> на пуле EP-Analytics.
/// Нельзя зарегистрировать второй <c>IDbContextFactory&lt;AppDbContext&gt;</c> — это отдельный контракт.
/// </summary>
public interface IAnalyticsDbContextFactory
{
    AppDbContext CreateDbContext();

    Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
