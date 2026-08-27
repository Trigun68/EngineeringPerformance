namespace EngineeringPerformance.Application.Auth;

public enum AuthLoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
    MfaRequired,
}

/// <summary>Пользователь + права для ответа login/me/refresh.</summary>
public sealed record AuthUserInfo(
    int Id,
    string Username,
    string DisplayName,
    int RoleId,
    string RoleName,
    string? WindowsUsername,
    bool IsActive,
    bool MfaEnabled,
    bool MustChangePassword,
    Dictionary<string, int> Permissions);

/// <summary>Выпущенная пара токенов; cookie выставляет только слой Api.</summary>
public sealed record IssuedTokens(string AccessToken, DateTime ExpiresAt, RefreshTokenIssue RefreshToken);

public sealed record AuthLoginResult(
    AuthLoginStatus Status,
    AuthUserInfo? User = null,
    IssuedTokens? Tokens = null,
    string? MfaSessionToken = null,
    DateTime? MfaExpiresAt = null,
    bool RequiresMfaSetup = false)
{
    public static AuthLoginResult Fail(AuthLoginStatus status) => new(status);
}

public sealed record AuthRefreshResult(AuthUserInfo User, IssuedTokens Tokens);

/// <summary>
/// Сценарии аутентификации: логин по паролю, Windows auto-login, refresh, MFA-завершение.
/// Контроллеры держат только HTTP-концерны (cookie, заголовки, IP), вся логика — здесь.
/// </summary>
public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(
        string username,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>Auto-login по Windows-имени (SPNEGO/dev-simulation). null — пользователь не найден.</summary>
    Task<AuthLoginResult?> WindowsLoginAsync(
        string windowsUsername,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>Ротация refresh-токена. null — токен невалиден.</summary>
    Task<AuthRefreshResult?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>Завершение MFA-входа по session-токену и коду. null — отказ (невалидный токен/код).</summary>
    Task<AuthLoginResult?> CompleteMfaLoginAsync(
        string mfaSessionToken,
        string code,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthUserInfo?> GetUserInfoAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Отображаемое имя для подсказки Windows-входа; null — identity не найдена.</summary>
    Task<string?> GetDisplayNameForLoginIdentityAsync(string input, CancellationToken cancellationToken = default);
}
