using System.Security.Claims;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Configuration;
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
using EngineeringPerformance.Api.Diagnostics;
using EngineeringPerformance.Api.Middleware;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserActionLogger _actionLogger;
    private readonly IHostEnvironment _environment;
    private readonly SpnegoOptions _spnegoOptions;
    private readonly DevWindowsAuthOptions _devWindowsAuth;

    public AuthController(
        AppDbContext context,
        IJwtTokenService jwtTokenService,
        IUserActionLogger actionLogger,
        IHostEnvironment environment,
        IOptions<SpnegoOptions> spnegoOptions,
        IOptions<DevWindowsAuthOptions> devWindowsAuth)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _actionLogger = actionLogger;
        _environment = environment;
        _spnegoOptions = spnegoOptions.Value;
        _devWindowsAuth = devWindowsAuth.Value;
    }

    public sealed record LoginRequest(string Username, string Password);

    public sealed record LoginResponse(DateTime ExpiresAt, UserInfoResponse User);

    public sealed record UserInfoResponse(
        int Id,
        string Username,
        string DisplayName,
        int RoleId,
        string RoleName,
        string? WindowsUsername,
        bool IsActive,
        Dictionary<string, int> Permissions);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserForLoginAsync(request.Username, cancellationToken);

        // #region agent log
        DebugSessionLog.Write(
            "A",
            "AuthController.Login",
            "login attempt",
            new
            {
                username = request.Username,
                userFound = user is not null,
                hasPasswordHash = !string.IsNullOrEmpty(user?.PasswordHash),
                hashPrefix = user?.PasswordHash is { Length: >= 20 } h ? h[..20] : null,
            });
        // #endregion

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized();

        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        // #region agent log
        DebugSessionLog.Write(
            "A",
            "AuthController.Login",
            passwordOk ? "password ok" : "password mismatch",
            new { username = user.Username, passwordOk });
        // #endregion
        if (!passwordOk)
        {
            await _actionLogger.LogForUserAsync(
                user.Id,
                UserActionType.LoginFailed,
                description: "Invalid password");
            return Unauthorized();
        }

        return Ok(await CreateLoginResponseAsync(user, $"Manual login: {user.Username}", cancellationToken));
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
        var autoLogin = await TryWindowsAutoLoginAsync(cancellationToken);
        return autoLogin is null ? Unauthorized() : Ok(autoLogin);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userIdRaw
            && int.TryParse(userIdRaw, out var userId))
        {
            await _actionLogger.LogForUserAsync(userId, UserActionType.Logout, cancellationToken: cancellationToken);
        }

        Response.Cookies.Delete(AuthCookieConstants.TokenCookieName, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
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
        var token = _jwtTokenService.GenerateToken(user);
        var expires = _jwtTokenService.GetExpiration();
        AppendAuthCookie(token, expires);

        return Ok(new LoginResponse(expires, MapUser(user, permissions)));
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
        var users = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);

        return users.FirstOrDefault(u => LoginIdentityMatcher.Matches(u, input));
    }

    private async Task<LoginResponse> CreateLoginResponseAsync(
        User user,
        string logDescription,
        CancellationToken cancellationToken)
    {
        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user);
        var expires = _jwtTokenService.GetExpiration();

        AppendAuthCookie(token, expires);

        await _actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.Login,
            description: logDescription,
            cancellationToken: cancellationToken);

        return new LoginResponse(expires, MapUser(user, permissions));
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

    private void AppendAuthCookie(string token, DateTime expires)
    {
        Response.Cookies.Append(AuthCookieConstants.TokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = _environment.IsProduction() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/",
        });
    }

    private async Task<Dictionary<string, int>> LoadPermissionsAsync(int roleId, string roleName, CancellationToken cancellationToken)
    {
        if (string.Equals(roleName, nameof(UserRole.Administrator), StringComparison.Ordinal))
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

    private static UserInfoResponse MapUser(Domain.Entities.User user, Dictionary<string, int> permissions) =>
        new(
            user.Id,
            user.Username,
            user.DisplayName,
            user.RoleId,
            user.Role.Name,
            user.WindowsUsername,
            user.IsActive,
            permissions);
}
