using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/users/me/settings")]
[Authorize]
public sealed class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsService _settings;
    private readonly ICurrentUserService _currentUser;

    public UserSettingsController(IUserSettingsService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<object>> Get(string key, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        var value = await _settings.GetAsync(userId, key, cancellationToken);
        return value is null ? NotFound() : Ok(new { key, value });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Put(string key, [FromBody] SettingValueRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        await _settings.PutAsync(userId, key, request.Value, cancellationToken);
        return NoContent();
    }

    public sealed record SettingValueRequest(string Value);
}
