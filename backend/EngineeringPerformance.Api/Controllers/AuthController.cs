using System.Security.Claims;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Api.Metrics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EngineeringPerformance.Api.Middleware;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthAuditService _authAuditService;
    private readonly AuthCookieHelper _authCookieHelper;
    private readonly IUserActionLogger _actionLogger;
    private readonly IHostEnvironment _environment;
    private readonly SpnegoOptions _spnegoOptions;
    private readonly DevWindowsAuthOptions _devWindowsAuth;
    private readonly ITrustedProxyValidator _trustedProxy;

    public AuthController(
        IAuthService authService,
        IRefreshTokenService refreshTokenService,
        IAuthAuditService authAuditService,
        AuthCookieHelper authCookieHelper,
        IUserActionLogger actionLogger,
        IHostEnvironment environment,
        IOptions<SpnegoOptions> spnegoOptions,
        IOptions<DevWindowsAuthOptions> devWindowsAuth,
        ITrustedProxyValidator trustedProxy)
    {
        _authService = authService;
        _refreshTokenService = refreshTokenService;
        _authAuditService = authAuditService;
        _authCookieHelper = authCookieHelper;
        _actionLogger = actionLogger;
        _environment = environment;
        _spnegoOptions = spnegoOptions.Value;
        _devWindowsAuth = devWindowsAuth.Value;
        _trustedProxy = trustedProxy;
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
        var result = await _authService.LoginAsync(
            request.Username,
            request.Password,
            GetRemoteIpAddress(),
            GetUserAgent(),
            cancellationToken);

        switch (result.Status)
        {
            case AuthLoginStatus.Success:
                AuthMetrics.LoginSuccess.Inc();
                _authCookieHelper.Append(
                    Response,
                    result.Tokens!.AccessToken,
                    result.Tokens.ExpiresAt,
                    result.Tokens.RefreshToken);
                return Ok(ToLoginResponse(result));
            case AuthLoginStatus.MfaRequired:
                AuthMetrics.LoginSuccess.Inc();
                return Ok(new LoginResponse(
                    result.MfaExpiresAt ?? DateTime.UtcNow,
                    MapUser(result.User!),
                    RequiresMfa: true,
                    MfaSessionToken: result.MfaSessionToken));
            default:
                AuthMetrics.LoginFailure.Inc();
                return Unauthorized();
        }
    }

    public sealed record WindowsIdentityHintResponse(string? WindowsUsername, string? DisplayName, string Source);

    [HttpGet("windows-identity")]
    [AllowAnonymous]
    public async Task<ActionResult<WindowsIdentityHintResponse>> WindowsIdentity(CancellationToken cancellationToken)
    {
        var hint = ResolveWindowsIdentityHintForDisplay();
        if (string.IsNullOrWhiteSpace(hint.Username))
            return Ok(new WindowsIdentityHintResponse(null, null, "none"));

        var displayName = await _authService.GetDisplayNameForLoginIdentityAsync(hint.Username, cancellationToken);
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

        var hint = ResolveWindowsIdentityHint();
        if (string.IsNullOrWhiteSpace(hint.Username))
            return Unauthorized();

        var result = await _authService.WindowsLoginAsync(
            hint.Username,
            GetRemoteIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (result is null)
            return Unauthorized();

        if (result.Status == AuthLoginStatus.MfaRequired)
        {
            return Ok(new LoginResponse(
                result.MfaExpiresAt ?? DateTime.UtcNow,
                MapUser(result.User!),
                RequiresMfa: true,
                MfaSessionToken: result.MfaSessionToken));
        }

        _authCookieHelper.Append(
            Response,
            result.Tokens!.AccessToken,
            result.Tokens.ExpiresAt,
            result.Tokens.RefreshToken);
        return Ok(ToLoginResponse(result));
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

        var result = await _authService.RefreshAsync(
            refreshToken,
            GetRemoteIpAddress(),
            GetUserAgent(),
            cancellationToken);
        if (result is null)
        {
            _authCookieHelper.Delete(Response);
            return Unauthorized();
        }

        _authCookieHelper.Append(
            Response,
            result.Tokens.AccessToken,
            result.Tokens.ExpiresAt,
            result.Tokens.RefreshToken);

        AuthMetrics.TokenRefresh.Inc();
        return Ok(new LoginResponse(
            result.Tokens.ExpiresAt,
            MapUser(result.User),
            AccessToken: result.Tokens.AccessToken,
            TokenType: "Bearer"));
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await _refreshTokenService.RevokeAllAsync(userId, "Logout all devices", cancellationToken);
        await _actionLogger.LogForUserAsync(userId, UserActionType.Logout, cancellationToken: cancellationToken);
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
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var info = await _authService.GetUserInfoAsync(userId, cancellationToken);
        return info is null ? Unauthorized() : Ok(MapUser(info));
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

    private bool IsTrustedProxyRequest()
    {
        Request.Headers.TryGetValue(SpnegoOptions.TrustedProxyTokenHeader, out var tokenValues);
        return _trustedProxy.IsTrustedProxyToken(tokenValues.FirstOrDefault());
    }

    private string? GetRefreshCookie() =>
        Request.Cookies.TryGetValue(AuthCookieConstants.RefreshCookieName, out var token) ? token : null;

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    internal static LoginResponse ToLoginResponse(AuthLoginResult result) =>
        new(
            result.Tokens!.ExpiresAt,
            MapUser(result.User!),
            RequiresMfaSetup: result.RequiresMfaSetup,
            AccessToken: result.Tokens.AccessToken,
            TokenType: "Bearer");

    internal static UserInfoResponse MapUser(AuthUserInfo user) =>
        new(
            user.Id,
            user.Username,
            user.DisplayName,
            user.RoleId,
            user.RoleName,
            user.WindowsUsername,
            user.IsActive,
            user.MfaEnabled,
            user.MustChangePassword,
            user.Permissions);
}
