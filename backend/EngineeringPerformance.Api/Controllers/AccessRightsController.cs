using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/access-rights")]
[Authorize]
public sealed class AccessRightsController : ControllerBase
{
    private readonly IAccessRightsService _accessRights;
    private readonly ICurrentUserService _currentUser;

    public AccessRightsController(IAccessRightsService accessRights, ICurrentUserService currentUser)
    {
        _accessRights = accessRights;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccessRightDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!IsAdministrator())
            return Forbid();

        return Ok(await _accessRights.GetAllAsync(cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAccessRightRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAdministrator())
            return Forbid();

        var updated = await _accessRights.UpdateAsync(id, request.AccessLevel, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    private bool IsAdministrator() =>
        PermissionObjectCatalog.IsSuperAdmin(_currentUser.RoleName, _currentUser.RoleId);
}
