namespace EngineeringPerformance.Core.Services;

public interface IDbConnectionService
{
    Task<bool> TestConnectionAsync(string connectionString);

    void SaveConnectionString(string connectionString);

    string GetCurrentConnectionString();
}
