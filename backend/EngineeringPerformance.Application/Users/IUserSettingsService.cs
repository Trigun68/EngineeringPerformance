namespace EngineeringPerformance.Application.Users;

public interface IUserSettingsService
{
    Task<string?> GetAsync(int userId, string key, CancellationToken cancellationToken = default);

    Task PutAsync(int userId, string key, string value, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetAllAsync(int userId, CancellationToken cancellationToken = default);

    Task PutManyAsync(int userId, IReadOnlyDictionary<string, string> preferences, CancellationToken cancellationToken = default);
}
