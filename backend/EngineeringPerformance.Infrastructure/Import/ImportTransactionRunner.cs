using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Повтор при EnableRetryOnFailure: пользовательская транзакция должна идти через CreateExecutionStrategy.
/// </summary>
internal static class ImportTransactionRunner
{
    /// <summary>
    /// Создаёт контекст, открывает транзакцию внутри execution strategy и коммитит при успехе.
    /// </summary>
    public static async Task<T> ExecuteWithRetryableTransactionAsync<T>(
        IDbContextFactory<AppDbContext> contextFactory,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var result = await action(context);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
