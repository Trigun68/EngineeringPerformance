using EngineeringPerformance.Core.Services;
using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class DbConnectionService : IDbConnectionService
{
    private readonly IAppSettings _settings;

    public DbConnectionService(IAppSettings settings)
    {
        _settings = settings;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SaveConnectionString(string connectionString)
    {
        _settings.SaveConnectionString(connectionString);
    }

    public string GetCurrentConnectionString()
    {
        return _settings.ConnectionString;
    }
}
