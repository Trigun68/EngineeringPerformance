using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Infrastructure.Persistence;

/// <summary>
/// Таймаут открытия соединения / TDS login — перегруз SQL, не баг запроса prefs.
/// Не ретраить и отвечать 503, чтобы не усиливать stampede.
/// </summary>
public static class SqlConnectionTimeoutClassifier
{
    public static bool IsUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;

            if (current is SqlException sql && IsTimeoutNumber(sql.Number))
                return true;

            if (current.Message.Contains("Connection Timeout Expired", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static bool IsTimeoutNumber(int number) =>
        number is -2 or -1 or 258 or 233 or 64;
}
