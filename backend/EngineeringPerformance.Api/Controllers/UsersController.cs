using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserLookupService _lookup;
    private readonly IUserManagementService _users;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        IUserLookupService lookup,
        IUserManagementService users,
        ICurrentUserService currentUser)
    {
        _lookup = lookup;
        _users = users;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserProfileDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return NotFound();

        // Имя из БД; при Windows Auth — fallback на Identity.Name / claims.
        var identityName = User.Identity?.Name;
        var displayName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : !string.IsNullOrWhiteSpace(identityName)
                ? identityName
                : user.Username;
        var windowsAccount = !string.IsNullOrWhiteSpace(user.WindowsUsername)
            ? user.WindowsUsername
            : identityName;

        return Ok(new CurrentUserProfileDto(
            user.Id,
            user.Username,
            displayName,
            windowsAccount,
            user.RoleName,
            user.IsActive));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeOwnPassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.OldPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword)
            || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest("Все поля пароля обязательны");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest("Новый пароль и подтверждение не совпадают");

        var (success, error) = await _users.ChangeOwnPasswordAsync(
            userId,
            request.OldPassword,
            request.NewPassword,
            cancellationToken);

        if (!success)
            return error == "Пользователь не найден" ? NotFound(error) : BadRequest(error);

        return NoContent();
    }

    [HttpPost("{id:int}/change-password")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<IActionResult> ChangePasswordForUser(
        int id,
        [FromBody] AdminChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("Новый пароль обязателен");

        var (success, error) = await _users.ChangePasswordForUserAsync(id, request.NewPassword, cancellationToken);
        if (!success)
            return error == "Пользователь не найден" ? NotFound(error) : BadRequest(error);

        return NoContent();
    }

    [HttpGet]
    [HasPermission(PermissionType.Users, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _users.GetAllAsync(includeArchived, cancellationToken));

    [HttpGet("roles")]
    [HasPermission(PermissionType.Users, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken = default) =>
        Ok(await _users.GetRolesAsync(cancellationToken));

    [HttpGet("lookup")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<IReadOnlyList<UserLookupDto>>> Lookup(CancellationToken cancellationToken) =>
        Ok(await _lookup.GetActiveUsersAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [HasPermission(PermissionType.Users, AccessLevel.Read)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, error) = await _users.CreateAsync(request, cancellationToken);
        if (error is not null)
            return Conflict(error);
        return CreatedAtAction(nameof(GetById), new { id = user!.Id }, user);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, error) = await _users.UpdateAsync(id, request, cancellationToken);
        if (error is not null)
            return error == "Пользователь не найден" ? NotFound(error) : Conflict(error);
        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var (success, error) = await _users.DeleteAsync(id, cancellationToken);
        if (!success)
            return error == "Пользователь не найден" ? NotFound(error) : Conflict(error);
        return NoContent();
    }

    [HttpPost("{id:int}/archive")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken = default)
    {
        var archived = await _users.ArchiveAsync(id, cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/unarchive")]
    [HasPermission(PermissionType.Users, AccessLevel.Edit)]
    public async Task<IActionResult> Unarchive(int id, CancellationToken cancellationToken = default)
    {
        var unarchived = await _users.UnarchiveAsync(id, cancellationToken);
        return unarchived ? NoContent() : NotFound();
    }
}
