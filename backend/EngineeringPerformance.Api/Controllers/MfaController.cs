using System.Security.Claims;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/auth/mfa")]
public sealed class MfaController(
    AppDbContext context,
    IMfaService mfaService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthAuditService authAuditService,
    AuthCookieHelper authCookieHelper) : ControllerBase
{
    public sealed record SetupResponse(string Secret, string QrCodeUri);
    public sealed record VerifySetupRequest(string Code, string Secret);
    public sealed record VerifyRequest(string Code, string MfaSessionToken);
    public sealed record DisableRequest(string Password);
    public sealed record StatusResponse(bool IsEnabled, bool IsRequired);

    [HttpPost("setup")]
    [Authorize]
    public async Task<ActionResult<SetupResponse>> Setup(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
            return Unauthorized();

        var secret = mfaService.GenerateSecret();
        await authAuditService.WriteAsync(
            AuthEventType.MfaSetupStarted,
            success: true,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);

        return Ok(new SetupResponse(secret, mfaService.GetQrCodeUri(user, secret)));
    }

    [HttpPost("verify-setup")]
    [Authorize]
    [EnableRateLimiting("mfa")]
    public async Task<IActionResult> VerifySetup([FromBody] VerifySetupRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
            return Unauthorized();

        if (!mfaService.VerifyTotpSecret(request.Secret, request.Code))
        {
            await WriteVerifyFailureAsync(user, cancellationToken);
            return BadRequest(new { message = "Неверный код подтверждения." });
        }

        await mfaService.EnableMfaAsync(user, request.Secret, cancellationToken);
        await authAuditService.WriteAsync(
            AuthEventType.MfaSetupCompleted,
            success: true,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("mfa")]
    public async Task<ActionResult<AuthController.LoginResponse>> Verify(
        [FromBody] VerifyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = jwtTokenService.ValidateMfaSessionToken(request.MfaSessionToken);
        if (userId is null)
            return Unauthorized();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null || !mfaService.IsMfaEnabled(user) || !mfaService.VerifyTotp(user, request.Code))
        {
            if (user is not null)
                await WriteVerifyFailureAsync(user, cancellationToken);
            return Unauthorized();
        }

        var permissions = await LoadPermissionsAsync(user.RoleId, user.Role.Name, cancellationToken);
        var expires = jwtTokenService.GetExpiration();
        var refreshToken = await refreshTokenService.CreateAsync(
            user.Id,
            GetRemoteIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        var accessToken = jwtTokenService.GenerateToken(user);
        authCookieHelper.Append(Response, accessToken, expires, refreshToken);

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

        return Ok(new AuthController.LoginResponse(
            expires,
            AuthController.MapUser(user, permissions),
            AccessToken: accessToken,
            TokenType: "Bearer"));
    }

    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] DisableRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
            return Unauthorized();

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest(new { message = "Неверный пароль." });

        await mfaService.DisableMfaAsync(user, cancellationToken);
        await authAuditService.WriteAsync(
            AuthEventType.MfaDisabled,
            success: true,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPost("reset/{userId:int}")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<IActionResult> Reset(int userId, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return NotFound();

        await mfaService.ResetMfaAsync(user, cancellationToken);
        await authAuditService.WriteAsync(
            AuthEventType.MfaReset,
            success: true,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<StatusResponse>> Status(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(new StatusResponse(mfaService.IsMfaEnabled(user), mfaService.IsMfaRequired(user)));
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsedUserId)
            ? await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == parsedUserId && u.IsActive, cancellationToken)
            : null;
    }

    private async Task WriteVerifyFailureAsync(User user, CancellationToken cancellationToken) =>
        await authAuditService.WriteAsync(
            AuthEventType.MfaVerifyFailed,
            success: false,
            user.Id,
            user.Username,
            cancellationToken: cancellationToken);

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

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
}
