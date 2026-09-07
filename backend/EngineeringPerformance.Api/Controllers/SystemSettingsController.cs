using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Settings;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>
/// Системные настройки. GET читает любой авторизованный пользователь (AppLayout — heartbeat
/// таймаута сессии); изменение — только объект Administration (SuperAdmin).
/// </summary>
[ApiController]
[Route("api/admin/system-settings")]
[Authorize]
public sealed class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settings;

    public SystemSettingsController(ISystemSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var minutes = await _settings.GetSessionExpirationMinutesAsync(cancellationToken);
        return Ok(new SystemSettingsDto(minutes));
    }

    [HttpPut]
    [HasPermission(PermissionType.Administration, AccessLevel.Edit)]
    public async Task<IActionResult> Put([FromBody] SystemSettingsDto dto, CancellationToken cancellationToken)
    {
        await _settings.SetSessionExpirationMinutesAsync(dto.SessionExpirationMinutes, cancellationToken);
        return NoContent();
    }
}

public sealed record SystemSettingsDto(int SessionExpirationMinutes);
