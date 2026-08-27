using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

public interface IAccountLockoutService
{
    Task<bool> CheckLockoutAsync(User user, CancellationToken cancellationToken = default);

    Task RecordFailedAttemptAsync(User user, CancellationToken cancellationToken = default);

    Task ResetFailedAttemptsAsync(User user, CancellationToken cancellationToken = default);

    Task UnlockAsync(User user, CancellationToken cancellationToken = default);
}
