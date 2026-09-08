using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Auth;

/// <summary>
/// Сценарии аутентификации (логин, Windows auto-login, refresh, MFA-завершение, /me).
/// HTTP-концерны (cookie, заголовки, IP) остаются в контроллерах.
/// </summary>
public sealed class AuthService(
    AppDbContext context,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IAccountLockoutService accountLockoutService,
    IMfaService mfaService,
    IAuthAuditService authAuditService,
    IUserActionLogger actionLogger) : IAuthService
{
    public async Task<AuthLoginResult> LoginAsync(
        string username,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserForLoginAsync(username, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            await authAuditService.WriteAsync(
                AuthEventType.LoginFailed,
                success: false,
                identifier: username,
                details: "Invalid credentials",
                cancellationToken: cancellationToken);
            return AuthLoginResult.Fail(AuthLoginStatus.InvalidCredentials);
        }

        if (await accountLockoutService.CheckLockoutAsync(user, cancellationToken))
        {
            await authAuditService.WriteAsync(
                AuthEventType.LoginLockedOut,
                success: false,
                user.Id,
                user.Username,
                details: "Account is locked",
                cancellationToken: cancellationToken);
            return AuthLoginResult.Fail(AuthLoginStatus.LockedOut);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await accountLockoutService.RecordFailedAttemptAsync(user, cancellationToken);
            await actionLogger.LogForUserAsync(
                user.Id,
                UserActionType.LoginFailed,
                description: "Invalid password");
            await authAuditService.WriteAsync(
                AuthEventType.LoginFailed,
                success: false,
                user.Id,
                user.Username,
                details: "Invalid credentials",
                cancellationToken: cancellationToken);
            return AuthLoginResult.Fail(AuthLoginStatus.InvalidCredentials);
        }

        await accountLockoutService.ResetFailedAttemptsAsync(user, cancellationToken);
        return await CreateLoginResultAsync(
            user,
            $"Manual login: {user.Username}",
            AuthEventType.LoginSuccess,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<AuthLoginResult?> WindowsLoginAsync(
        string windowsUsername,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserForLoginAsync(windowsUsername, cancellationToken);
        if (user is null)
            return null;

        return await CreateLoginResultAsync(
            user,
            $"Auto-login: {windowsUsername}",
            AuthEventType.NegotiateSuccess,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<AuthRefreshResult?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var rotation = await refreshTokenService.ValidateAndRotateAsync(
            refreshToken,
            ipAddress,
            userAgent,
            cancellationToken);
        if (rotation is null)
        {
            await authAuditService.WriteAsync(
                AuthEventType.RefreshFailed,
                success: false,
                details: "Refresh token is invalid",
                cancellationToken: cancellationToken);
            return null;
        }

        var permissions = await LoadPermissionsAsync(rotation.User.RoleId, rotation.User.Role.Name, cancellationToken);
        var token = jwtTokenService.GenerateToken(rotation.User);
        var expires = jwtTokenService.GetExpiration();

        await authAuditService.WriteAsync(
            AuthEventType.RefreshSuccess,
            success: true,
            userId: rotation.User.Id,
            identifier: rotation.User.Username,
            cancellationToken: cancellationToken);

        return new AuthRefreshResult(
            MapUser(rotation.User, permissions),
            new IssuedTokens(token, expires, new RefreshTokenIssue(rotation.Token, rotation.ExpiresAt)));
    }

    public async Task<AuthLoginResult?> CompleteMfaLoginAsync(
        string mfaSessionToken,
        string code,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var userId = jwtTokenService.ValidateMfaSessionToken(mfaSessionToken);
        if (userId is null)
            return null;

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null || !mfaService.IsMfaEnabled(user) || !mfaService.VerifyTotp(user, code))
        {
            if (user is not null)
            {
                await authAuditService.WriteAsync(
                    AuthEventType.MfaVerifyFailed,
                    success: false,
                    user.Id,
                    user.Username,
                    cancellationToken: cancellationToken);
            }

            return null;
        }

        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        var expires = jwtTokenService.GetExpiration();
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, ipAddress, userAgent, cancellationToken);
        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var accessToken = jwtTokenService.GenerateToken(user);

        await actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.Login,
            description: $"MFA login: {user.Username}",
            cancellationToken: cancellationToken);
        await authAuditService.WriteAsync(
            AuthEventType.MfaVerifySuccess,
            success: true,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);
        await authAuditService.WriteAsync(
            AuthEventType.LoginSuccess,
            success: true,
            user.Id,
            user.Username,
            details: "MFA login completed",
            cancellationToken: cancellationToken);

        return new AuthLoginResult(
            AuthLoginStatus.Success,
            MapUser(user, permissions),
            new IssuedTokens(accessToken, expires, refreshToken));
    }

    public async Task<AuthUserInfo?> GetUserInfoAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        return user is null ? null : MapUser(user, await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken));
    }

    public async Task<string?> GetDisplayNameForLoginIdentityAsync(string input, CancellationToken cancellationToken = default)
    {
        var user = await FindUserForLoginAsync(input, cancellationToken);
        return !string.IsNullOrWhiteSpace(user?.DisplayName) ? user!.DisplayName : null;
    }

    private async Task<AuthLoginResult> CreateLoginResultAsync(
        User user,
        string logDescription,
        AuthEventType authEventType,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        if (mfaService.IsMfaEnabled(user))
        {
            return new AuthLoginResult(
                AuthLoginStatus.MfaRequired,
                MapUser(user, permissions),
                MfaSessionToken: jwtTokenService.GenerateMfaSessionToken(user),
                MfaExpiresAt: jwtTokenService.GetExpiration());
        }

        var token = jwtTokenService.GenerateToken(user);
        var expires = jwtTokenService.GetExpiration();
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, ipAddress, userAgent, cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        await actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.Login,
            description: logDescription,
            cancellationToken: cancellationToken);
        await authAuditService.WriteAsync(
            authEventType,
            success: true,
            userId: user.Id,
            identifier: user.Username,
            details: logDescription,
            cancellationToken: cancellationToken);

        return new AuthLoginResult(
            AuthLoginStatus.Success,
            MapUser(user, permissions),
            new IssuedTokens(token, expires, refreshToken),
            RequiresMfaSetup: mfaService.IsMfaRequired(user));
    }

    private async Task<User?> FindUserForLoginAsync(string input, CancellationToken cancellationToken)
    {
        // EndsWith + StringComparison не транслируется в SQL Server — используем Like (суффикс \sam).
        var windowsSuffix = $@"%\{input}";
        return await context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .Where(u => u.Username == input
                     || u.WindowsUsername == input
                     || (u.WindowsUsername != null && EF.Functions.Like(u.WindowsUsername, windowsSuffix)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<string, int>> LoadPermissionsAsync(
        int roleId,
        string roleName,
        CancellationToken cancellationToken)
    {
        if (PermissionObjectCatalog.IsSuperAdmin(roleName, roleId))
        {
            return PermissionObjectCatalog.AllObjectNames
                .ToDictionary(name => name, _ => (int)AccessLevel.Edit);
        }

        var rights = await context.AccessRights
            .AsNoTracking()
            .Where(r => r.RoleId == roleId)
            .ToListAsync(cancellationToken);

        return rights.ToDictionary(r => r.ObjectName, r => (int)r.AccessLevel);
    }

    private static AuthUserInfo MapUser(User user, Dictionary<string, int> permissions) =>
        new(
            user.Id,
            user.Username,
            user.DisplayName,
            user.RoleId,
            user.Role.Name,
            user.WindowsUsername,
            user.IsActive,
            user.MfaEnabled,
            user.MustChangePassword,
            permissions);
}
