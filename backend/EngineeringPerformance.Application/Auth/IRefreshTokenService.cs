using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

public sealed record RefreshTokenIssue(string Token, DateTime ExpiresAt);

public sealed record RefreshTokenRotation(User User, string Token, DateTime ExpiresAt);

public sealed record RefreshSession(
    int Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? IpAddress,
    string? UserAgent);

public interface IRefreshTokenService
{
    Task<RefreshTokenIssue> CreateAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRotation?> ValidateAndRotateAsync(
        string token,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string token,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        int userId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshSession>> GetSessionsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
