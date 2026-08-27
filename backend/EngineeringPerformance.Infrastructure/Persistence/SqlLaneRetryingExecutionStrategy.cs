using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace EngineeringPerformance.Infrastructure.Persistence;

/// <summary>
/// Ретраи deadlock/transport; не ретраить connection timeout (-2) — каждый Open() усиливает stampede.
/// </summary>
internal sealed class SqlLaneRetryingExecutionStrategy : ExecutionStrategy
{
    public SqlLaneRetryingExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount,
        TimeSpan maxRetryDelay)
        : base(dependencies, maxRetryCount, maxRetryDelay)
    {
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        if (SqlConnectionTimeoutClassifier.IsUnavailable(exception))
            return false;

        if (exception is SqlException sql)
        {
            foreach (SqlError error in sql.Errors)
            {
                if (IsTransientSqlError(error.Number))
                    return true;
            }
        }

        return exception is TimeoutException;
    }

    /// <summary>Подмножество transient-кодов EF, без таймаута соединения.</summary>
    private static bool IsTransientSqlError(int number) => number is
        20 or 0 or
        1204 or 1205 or 1222 or
        49918 or 49919 or 49920 or
        4060 or 40197 or 40501 or 40613 or
        10928 or 10929 or
        40143 or 41301 or 41302 or 41305 or 41325 or 41839;
}
