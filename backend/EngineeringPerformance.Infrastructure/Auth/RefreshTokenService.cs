using System.Security.Cryptography;
using System.Text;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class RefreshTokenService(
    AppDbContext context,
    IOptions<JwtSettings> settings) : IRefreshTokenService
{
    private const string RotatedReason = "Rotated";

    /// <summary>
    /// Окно, в течение которого повторный refresh уже ротированным токеном считается
    /// конкурентным запросом (несколько вкладок с общей cookie jar), а не компрометацией.
    /// </summary>
    private static readonly TimeSpan RotationGracePeriod = TimeSpan.FromSeconds(30);

    private readonly AppDbContext _context = context;
    private readonly JwtSettings _settings = settings.Value;

    public async Task<RefreshTokenIssue> CreateAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var issue = CreateIssue();
        _context.RefreshTokens.Add(CreateEntity(userId, issue, ipAddress, userAgent));
        await _context.SaveChangesAsync(cancellationToken);
        return issue;
    }

    public async Task<RefreshTokenRotation?> ValidateAndRotateAsync(
        string token,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var existing = await _context.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .ThenInclude(user => user.Role)
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

        if (existing is null
            || existing.ExpiresAt <= DateTime.UtcNow
            || !existing.User.IsActive)
        {
            return null;
        }

        if (existing.RevokedAt is not null)
        {
            return await TryIssueForConcurrentRefreshAsync(existing, ipAddress, userAgent, cancellationToken);
        }

        var issue = CreateIssue();
        var replacement = CreateEntity(existing.UserId, issue, ipAddress, userAgent);
        _context.RefreshTokens.Add(replacement);

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevocationReason = RotatedReason;
        // Навигация вместо FK: EF подставит ReplacedByTokenId после генерации Id,
        // поэтому отзыв и замена сохраняются одним атомарным SaveChanges.
        existing.ReplacedByToken = replacement;
        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotation(existing.User, issue.Token, issue.ExpiresAt);
    }

    /// <summary>
    /// Вторая вкладка успела прислать refresh с тем же токеном сразу после ротации:
    /// выдаём новый токен вместо разлогина. Активную замену не отзываем — cookie jar
    /// общая, и валидным останется последний полученный браузером токен.
    /// </summary>
    private async Task<RefreshTokenRotation?> TryIssueForConcurrentRefreshAsync(
        RefreshToken rotated,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!IsWithinRotationGrace(rotated))
            return null;

        // После logout или отзыва цепочки повторное использование недопустимо.
        var replacementIsActive = rotated.ReplacedByTokenId is { } replacementId
            && await _context.RefreshTokens.AnyAsync(
                candidate => candidate.Id == replacementId
                    && candidate.RevokedAt == null
                    && candidate.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (!replacementIsActive)
            return null;

        var issue = CreateIssue();
        _context.RefreshTokens.Add(CreateEntity(rotated.UserId, issue, ipAddress, userAgent));
        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotation(rotated.User, issue.Token, issue.ExpiresAt);
    }

    private static bool IsWithinRotationGrace(RefreshToken token) =>
        string.Equals(token.RevocationReason, RotatedReason, StringComparison.Ordinal)
        && token.RevokedAt is { } revokedAt
        && DateTime.UtcNow - revokedAt <= RotationGracePeriod;

    public async Task<bool> RevokeAsync(
        string token,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.RevokedAt is not null)
            return false;

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevocationReason = Trim(reason, 256);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllAsync(
        int userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId
                && token.RevokedAt == null
                && token.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
            return;

        var revokedAt = DateTime.UtcNow;
        foreach (var refreshToken in activeTokens)
        {
            refreshToken.RevokedAt = revokedAt;
            refreshToken.RevocationReason = Trim(reason, 256);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshSession>> GetSessionsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId
                && token.RevokedAt == null
                && token.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => new RefreshSession(
                token.Id,
                token.CreatedAt,
                token.ExpiresAt,
                token.IpAddress,
                token.UserAgent))
            .ToListAsync(cancellationToken);
    }

    private RefreshTokenIssue CreateIssue()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenIssue(token, DateTime.UtcNow.AddDays(GetRefreshLifetimeDays()));
    }

    private RefreshToken CreateEntity(
        int userId,
        RefreshTokenIssue issue,
        string? ipAddress,
        string? userAgent) =>
        new()
        {
            UserId = userId,
            TokenHash = HashToken(issue.Token),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = issue.ExpiresAt,
            IpAddress = Trim(ipAddress, 64),
            UserAgent = Trim(userAgent, 1024)
        };

    private int GetRefreshLifetimeDays() => Math.Clamp(_settings.RefreshTokenLifetimeDays, 1, 90);

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
