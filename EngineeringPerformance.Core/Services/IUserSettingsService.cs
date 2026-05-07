namespace EngineeringPerformance.Core.Services;

public interface IUserSettingsService
{
    Task<string?> GetAsync(int userId, string key);
    Task SetAsync(int userId, string key, string value);
    Task<Dictionary<string, string>> GetAllAsync(int userId);
}
