using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class AccountLockoutService(
    AppDbContext context,
    IOptions<RateLimitOptions> options) : IAccountLockoutService
{
    private readonly AppDbContext _context = context;
    private readonly RateLimitOptions _options = options.Value;

    public async Task<bool> CheckLockoutAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!user.IsLockedOut)
            return false;

        if (user.LockoutEndsAt is null || user.LockoutEndsAt > DateTime.UtcNow)
            return true;

        await UnlockAsync(user, cancellationToken);
        return false;
    }

    public async Task RecordFailedAttemptAsync(User user, CancellationToken cancellationToken = default)
    {
        user.FailedLoginAttempts++;
        user.LastFailedLoginAt = DateTime.UtcNow;

        if (user.FailedLoginAttempts >= GetMaxAttempts())
        {
            user.IsLockedOut = true;
            user.LockoutEndsAt = DateTime.UtcNow.AddMinutes(GetLockoutDurationMinutes());
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedAttemptsAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user.FailedLoginAttempts == 0
            && !user.IsLockedOut
            && user.LockoutEndsAt is null
            && user.LastFailedLoginAt is null)
        {
            return;
        }

        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndsAt = null;
        user.LastFailedLoginAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockAsync(User user, CancellationToken cancellationToken = default)
    {
        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndsAt = null;
        user.LastFailedLoginAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private int GetMaxAttempts() => Math.Max(1, _options.LoginMaxAttempts);

    private int GetLockoutDurationMinutes() => Math.Clamp(_options.LockoutDurationMinutes, 1, 1440);
}
