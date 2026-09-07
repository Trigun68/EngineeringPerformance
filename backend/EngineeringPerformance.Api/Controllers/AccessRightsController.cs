using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>
/// Права доступа по объектам. Объект <see cref="PermissionType.Administration"/> выдаётся только
/// SuperAdmin (bypass в PermissionService), поэтому [HasPermission(Administration)] ≡ «только SuperAdmin».
/// </summary>
[ApiController]
[Route("api/access-rights")]
[Authorize]
public sealed class AccessRightsController : ControllerBase
{
    private readonly IAccessRightsService _accessRights;

    public AccessRightsController(IAccessRightsService accessRights)
    {
        _accessRights = accessRights;
    }

    [HttpGet]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<AccessRightDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _accessRights.GetAllAsync(cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(PermissionType.Administration, AccessLevel.Edit)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAccessRightRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _accessRights.UpdateAsync(id, request.AccessLevel, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
