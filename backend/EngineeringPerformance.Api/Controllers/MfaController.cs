using System.Security.Claims;
using EngineeringPerformance.Api.Auth;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/auth/mfa")]
public sealed class MfaController(
    IMfaService mfaService,
    IUserManagementService users,
    IAuthService authService,
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
        var result = await authService.CompleteMfaLoginAsync(
            request.MfaSessionToken,
            request.Code,
            GetRemoteIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (result is null)
            return Unauthorized();

        authCookieHelper.Append(
            Response,
            result.Tokens!.AccessToken,
            result.Tokens.ExpiresAt,
            result.Tokens.RefreshToken);

        return Ok(AuthController.ToLoginResponse(result));
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
        var target = await users.GetByIdAsync(userId, cancellationToken);
        if (target is null)
            return NotFound();

        var result = await users.ResetMfaAsync(userId, cancellationToken);
        if (result == MfaResetStatus.Disabled)
            return BadRequest("Многофакторная аутентификация отключена.");
        if (result == MfaResetStatus.UserNotFound)
            return NotFound();

        await authAuditService.WriteAsync(
            AuthEventType.MfaReset,
            success: true,
            target.Id,
            target.Username,
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
            ? await mfaService.GetActiveUserByIdAsync(parsedUserId, cancellationToken)
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
}
