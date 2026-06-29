namespace EngineeringPerformance.Core.Services;

public interface IUserSettingsService
{
    Task<string?> GetAsync(int userId, string key);
    Task SetAsync(int userId, string key, string value);

    /// <summary>
    /// Синхронная запись настройки (для вызова при закрытии приложения с UI-потока).
    /// SQLite под капотом синхронный — нет смысла в async-обёртке при shutdown.
    /// </summary>
    void SetSync(int userId, string key, string value);

    Task<Dictionary<string, string>> GetAllAsync(int userId);
}
