using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/users/me/preferences")]
[Authorize]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly IUserSettingsService _settings;
    private readonly ICurrentUserService _currentUser;

    public UserPreferencesController(IUserSettingsService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, string>>> GetAll(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        var preferences = await _settings.GetAllAsync(userId, cancellationToken);
        return Ok(preferences);
    }

    [HttpPut]
    public async Task<IActionResult> Put(
        [FromBody] PutPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        if (request.Preferences is { Count: > 0 })
        {
            await _settings.PutManyAsync(userId, request.Preferences, cancellationToken);
            return NoContent();
        }

        if (!string.IsNullOrWhiteSpace(request.Key))
        {
            await _settings.PutAsync(userId, request.Key, request.Value ?? string.Empty, cancellationToken);
            return NoContent();
        }

        return BadRequest("Укажите preferences или key/value");
    }
}
