namespace EngineeringPerformance.Application.Settings;

public interface ISystemSettingsService
{
    Task<int> GetSessionExpirationMinutesAsync(CancellationToken cancellationToken = default);

    Task SetSessionExpirationMinutesAsync(int minutes, CancellationToken cancellationToken = default);
}
