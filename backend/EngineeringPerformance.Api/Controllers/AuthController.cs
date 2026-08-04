using System.Security.Claims;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Api.Metrics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using EngineeringPerformance.Api.Middleware;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccountLockoutService _accountLockoutService;
    private readonly IMfaService _mfaService;
    private readonly IAuthAuditService _authAuditService;
    private readonly AuthCookieHelper _authCookieHelper;
    private readonly IUserActionLogger _actionLogger;
    private readonly IHostEnvironment _environment;
    private readonly SpnegoOptions _spnegoOptions;
    private readonly DevWindowsAuthOptions _devWindowsAuth;

    public AuthController(
        AppDbContext context,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAccountLockoutService accountLockoutService,
        IMfaService mfaService,
        IAuthAuditService authAuditService,
        AuthCookieHelper authCookieHelper,
        IUserActionLogger actionLogger,
        IHostEnvironment environment,
        IOptions<SpnegoOptions> spnegoOptions,
        IOptions<DevWindowsAuthOptions> devWindowsAuth)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _accountLockoutService = accountLockoutService;
        _mfaService = mfaService;
        _authAuditService = authAuditService;
        _authCookieHelper = authCookieHelper;
        _actionLogger = actionLogger;
        _environment = environment;
        _spnegoOptions = spnegoOptions.Value;
        _devWindowsAuth = devWindowsAuth.Value;
    }

    public sealed record LoginRequest(string Username, string Password);

    public sealed record LoginResponse(
        DateTime ExpiresAt,
        UserInfoResponse User,
        bool RequiresMfa = false,
        string? MfaSessionToken = null,
        bool RequiresMfaSetup = false,
        string? AccessToken = null,
        string? TokenType = null);

    public sealed record UserInfoResponse(
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

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserForLoginAsync(request.Username, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            AuthMetrics.LoginFailure.Inc();
            await _authAuditService.WriteAsync(
                AuthEventType.LoginFailed,
                success: false,
                identifier: request.Username,
                details: "Invalid credentials",
                cancellationToken: cancellationToken);
            return Unauthorized();
        }

        if (await _accountLockoutService.CheckLockoutAsync(user, cancellationToken))
        {
            AuthMetrics.LoginFailure.Inc();
            await _authAuditService.WriteAsync(
                AuthEventType.LoginLockedOut,
                success: false,
                user.Id,
                user.Username,
                details: "Account is locked",
                cancellationToken: cancellationToken);
            return Unauthorized();
        }

        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordOk)
        {
            AuthMetrics.LoginFailure.Inc();
            await _accountLockoutService.RecordFailedAttemptAsync(user, cancellationToken);
            await _actionLogger.LogForUserAsync(
                user.Id,
                UserActionType.LoginFailed,
                description: "Invalid password");
            await _authAuditService.WriteAsync(
                AuthEventType.LoginFailed,
                success: false,
                user.Id,
                user.Username,
                details: "Invalid credentials",
                cancellationToken: cancellationToken);
            return Unauthorized();
        }

        await _accountLockoutService.ResetFailedAttemptsAsync(user, cancellationToken);
        AuthMetrics.LoginSuccess.Inc();
        return Ok(await CreateLoginResponseAsync(
            user,
            $"Manual login: {user.Username}",
            AuthEventType.LoginSuccess,
            cancellationToken));
    }

    public sealed record WindowsIdentityHintResponse(string? WindowsUsername, string? DisplayName, string Source);

    [HttpGet("windows-identity")]
    [AllowAnonymous]
    public async Task<ActionResult<WindowsIdentityHintResponse>> WindowsIdentity(CancellationToken cancellationToken)
    {
        var hint = ResolveWindowsIdentityHintForDisplay();
        if (string.IsNullOrWhiteSpace(hint.Username))
            return Ok(new WindowsIdentityHintResponse(null, null, "none"));

        var user = await FindUserForLoginAsync(hint.Username, cancellationToken);
        var displayName = !string.IsNullOrWhiteSpace(user?.DisplayName)
            ? user!.DisplayName
            : null;

        return Ok(new WindowsIdentityHintResponse(hint.Username, displayName, hint.Source));
    }

    public sealed record SpnegoProfileResponse(
        bool SpnegoEnabled,
        bool TrustedProxyRequest,
        bool RemoteUserHeaderPresent,
        string? RemoteUser,
        bool DevWindowsUserConfigured,
        string? AuthMethod);

    [HttpGet("spnego-profile")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public ActionResult<SpnegoProfileResponse> SpnegoProfile()
    {
        var trustedProxy = IsTrustedProxyRequest();
        var remoteUserPresent = Request.Headers.TryGetValue(SpnegoUserMiddleware.RemoteUserHeader, out var remoteUserValues);
        var remoteUser = remoteUserValues.FirstOrDefault()?.Trim();
        var authMethod = User.FindFirstValue("auth_method");

        return Ok(new SpnegoProfileResponse(
            SpnegoEnabled: _spnegoOptions.Enabled,
            TrustedProxyRequest: trustedProxy,
            RemoteUserHeaderPresent: remoteUserPresent,
            RemoteUser: string.IsNullOrWhiteSpace(remoteUser) ? null : remoteUser,
            DevWindowsUserConfigured: !string.IsNullOrWhiteSpace(_devWindowsAuth.SimulatedWindowsUser),
            AuthMethod: authMethod));
    }

    [HttpGet("negotiate")]
    [HttpPost("negotiate")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Negotiate(CancellationToken cancellationToken)
    {
        if (!_spnegoOptions.Enabled
            && (!_environment.IsDevelopment() || string.IsNullOrWhiteSpace(_devWindowsAuth.SimulatedWindowsUser)))
        {
            return NotFound();
        }

        var autoLogin = await TryWindowsAutoLoginAsync(cancellationToken);
        return autoLogin is null ? Unauthorized() : Ok(autoLogin);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshCookie();
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _refreshTokenService.RevokeAsync(refreshToken, "Logout", cancellationToken);

        if (User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userIdRaw
            && int.TryParse(userIdRaw, out var userId))
        {
            await _actionLogger.LogForUserAsync(userId, UserActionType.Logout, cancellationToken: cancellationToken);
            await _authAuditService.WriteAsync(
                AuthEventType.Logout,
                success: true,
                userId: userId,
                cancellationToken: cancellationToken);
        }

        _authCookieHelper.Delete(Response);
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshCookie();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authAuditService.WriteAsync(
                AuthEventType.RefreshFailed,
                success: false,
                details: "Refresh cookie is missing",
                cancellationToken: cancellationToken);
            return Unauthorized();
        }

        var rotation = await _refreshTokenService.ValidateAndRotateAsync(
            refreshToken,
            GetRemoteIpAddress(),
            GetUserAgent(),
            cancellationToken);
        if (rotation is null)
        {
            _authCookieHelper.Delete(Response);
            await _authAuditService.WriteAsync(
                AuthEventType.RefreshFailed,
                success: false,
                details: "Refresh token is invalid",
                cancellationToken: cancellationToken);
            return Unauthorized();
        }

        var permissions = await LoadPermissionsAsync(rotation.User.RoleId, rotation.User.Role.Name, cancellationToken);
        var token = _jwtTokenService.GenerateToken(rotation.User);
        var expires = _jwtTokenService.GetExpiration();
        _authCookieHelper.Append(Response, token, expires, new RefreshTokenIssue(rotation.Token, rotation.ExpiresAt));

        await _authAuditService.WriteAsync(
            AuthEventType.RefreshSuccess,
            success: true,
            userId: rotation.User.Id,
            identifier: rotation.User.Username,
            cancellationToken: cancellationToken);

        AuthMetrics.TokenRefresh.Inc();
        return Ok(new LoginResponse(
            expires,
            MapUser(rotation.User, permissions),
            AccessToken: token,
            TokenType: "Bearer"));
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await _refreshTokenService.RevokeAllAsync(userId, "Logout all devices", cancellationToken);
        await _authAuditService.WriteAsync(
            AuthEventType.LogoutAllDevices,
            success: true,
            userId: userId,
            cancellationToken: cancellationToken);
        _authCookieHelper.Delete(Response);
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<RefreshSession>>> GetSessions(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(await _refreshTokenService.GetSessionsAsync(userId, cancellationToken));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user is null)
            return Unauthorized();

        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        return Ok(MapUser(user, permissions));
    }

    private async Task<LoginResponse?> TryWindowsAutoLoginAsync(CancellationToken cancellationToken)
    {
        var hint = ResolveWindowsIdentityHint();
        if (string.IsNullOrWhiteSpace(hint.Username))
            return null;

        var user = await FindUserForLoginAsync(hint.Username, cancellationToken);
        if (user is null)
            return null;

        return await CreateLoginResponseAsync(
            user,
            $"Auto-login: {hint.Username}",
            AuthEventType.NegotiateSuccess,
            cancellationToken);
    }

    private (string? Username, string Source) ResolveWindowsIdentityHintForDisplay()
    {
        if (IsTrustedProxyRequest()
            && Request.Headers.TryGetValue(SpnegoUserMiddleware.RemoteUserHeader, out var remoteUserValues))
        {
            var remoteUser = remoteUserValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(remoteUser))
                return (remoteUser, "proxy");
        }

        // Dev-simulation (DEV_WINDOWS_USER) — только для negotiate, не для UI входа.
        return (null, "none");
    }

    private (string? Username, string Source) ResolveWindowsIdentityHint()
    {
        if (IsTrustedProxyRequest()
            && Request.Headers.TryGetValue(SpnegoUserMiddleware.RemoteUserHeader, out var remoteUserValues))
        {
            var remoteUser = remoteUserValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(remoteUser))
                return (remoteUser, "proxy");
        }

        if (_environment.IsDevelopment()
            && !string.IsNullOrWhiteSpace(_devWindowsAuth.SimulatedWindowsUser))
        {
            return (_devWindowsAuth.SimulatedWindowsUser.Trim(), "development");
        }

        return (null, "none");
    }

    private async Task<User?> FindUserForLoginAsync(string input, CancellationToken cancellationToken)
    {
        // EndsWith + StringComparison не транслируется в SQL Server — используем Like (суффикс \sam).
        var windowsSuffix = $@"%\{input}";
        return await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .Where(u => u.Username == input
                     || u.WindowsUsername == input
                     || (u.WindowsUsername != null && EF.Functions.Like(u.WindowsUsername, windowsSuffix)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<LoginResponse> CreateLoginResponseAsync(
        User user,
        string logDescription,
        AuthEventType authEventType,
        CancellationToken cancellationToken)
    {
        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        if (_mfaService.IsMfaEnabled(user))
        {
            return new LoginResponse(
                _jwtTokenService.GetExpiration(),
                MapLoginUser(user, permissions),
                RequiresMfa: true,
                MfaSessionToken: _jwtTokenService.GenerateMfaSessionToken(user));
        }

        var token = _jwtTokenService.GenerateToken(user);
        var expires = _jwtTokenService.GetExpiration();
        var refreshToken = await _refreshTokenService.CreateAsync(
            user.Id,
            GetRemoteIpAddress(),
            GetUserAgent(),
            cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _authCookieHelper.Append(Response, token, expires, refreshToken);

        await _actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.Login,
            description: logDescription,
            cancellationToken: cancellationToken);
        await _authAuditService.WriteAsync(
            authEventType,
            success: true,
            userId: user.Id,
            identifier: user.Username,
            details: logDescription,
            cancellationToken: cancellationToken);

        return new LoginResponse(
            expires,
            MapLoginUser(user, permissions),
            RequiresMfaSetup: _mfaService.IsMfaRequired(user),
            AccessToken: token,
            TokenType: "Bearer");
    }

    private bool IsTrustedProxyRequest()
    {
        if (string.IsNullOrWhiteSpace(_spnegoOptions.TrustedProxySecret))
            return false;

        if (!Request.Headers.TryGetValue(SpnegoOptions.TrustedProxyTokenHeader, out var tokenValues))
            return false;

        var provided = tokenValues.FirstOrDefault();
        return !string.IsNullOrEmpty(provided)
            && string.Equals(provided, _spnegoOptions.TrustedProxySecret, StringComparison.Ordinal);
    }

    private string? GetRefreshCookie() =>
        Request.Cookies.TryGetValue(AuthCookieConstants.RefreshCookieName, out var token) ? token : null;

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private async Task<Dictionary<string, int>> LoadPermissionsAsync(int roleId, string roleName, CancellationToken cancellationToken)
    {
        if (PermissionObjectCatalog.IsSuperAdmin(roleName, roleId))
        {
            return PermissionObjectCatalog.AllObjectNames
                .ToDictionary(name => name, _ => (int)AccessLevel.Edit);
        }

        var rights = await _context.AccessRights
            .AsNoTracking()
            .Where(r => r.RoleId == roleId)
            .ToListAsync(cancellationToken);

        return rights.ToDictionary(r => r.ObjectName, r => (int)r.AccessLevel);
    }

    public static UserInfoResponse MapUser(Domain.Entities.User user, Dictionary<string, int> permissions) =>
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

    private static UserInfoResponse MapLoginUser(Domain.Entities.User user, Dictionary<string, int> permissions) =>
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
