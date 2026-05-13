using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Маскирование строк подключения для логов и консоли — без утечки пароля SQL-аутентификации.
/// </summary>
public static class ConnectionStringMasking
{
    public static string MaskForDisplay(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
                builder.Password = "****";
            return builder.ConnectionString;
        }
        catch
        {
            return "[unparseable connection string]";
        }
    }
}
