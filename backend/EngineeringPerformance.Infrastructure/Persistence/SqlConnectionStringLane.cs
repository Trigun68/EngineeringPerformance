using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Infrastructure.Persistence;

/// <summary>
/// Два SqlClient-пула на одном Server/DB: разный <c>Application Name</c> = разные пулы.
/// Interactive — сессия и prefs; analytics — page-data, persist часов, телеметрия SQL.
/// </summary>
public static class SqlConnectionStringLane
{
    public const string InteractiveAppName = "EP-Interactive";
    public const string AnalyticsAppName = "EP-Analytics";

    public static string ForInteractive(string baseConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            ApplicationName = InteractiveAppName,
            MinPoolSize = 2,
            MaxPoolSize = 32,
            ConnectTimeout = 5,
        };
        return builder.ConnectionString;
    }

    public static string ForAnalytics(string baseConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            ApplicationName = AnalyticsAppName,
            MinPoolSize = 0,
            MaxPoolSize = 8,
            ConnectTimeout = 15,
        };
        return builder.ConnectionString;
    }
}
