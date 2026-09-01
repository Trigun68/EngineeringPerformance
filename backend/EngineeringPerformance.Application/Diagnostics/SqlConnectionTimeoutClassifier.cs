namespace EngineeringPerformance.Application.Diagnostics;

/// <summary>
/// Таймаут открытия соединения / TDS login — перегруз SQL, не баг запроса.
/// Не ретраить и отвечать 503, чтобы не усиливать stampede.
/// Без ссылки на Microsoft.Data.SqlClient (слой Application).
/// </summary>
public static class SqlConnectionTimeoutClassifier
{
    public static bool IsUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;

            if (current.Message.Contains("Connection Timeout Expired", StringComparison.OrdinalIgnoreCase))
                return true;

            var number = TryGetSqlErrorNumber(current);
            if (number is -2 or -1 or 258 or 233 or 64)
                return true;
        }

        return false;
    }

    internal static bool IsTimeoutNumber(int number) =>
        number is -2 or -1 or 258 or 233 or 64;

    private static int? TryGetSqlErrorNumber(Exception exception)
    {
        var type = exception.GetType();
        if (!string.Equals(type.Name, "SqlException", StringComparison.Ordinal))
            return null;

        var property = type.GetProperty("Number");
        return property?.GetValue(exception) is int number ? number : null;
    }
}
