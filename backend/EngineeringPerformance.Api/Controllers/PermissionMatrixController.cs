using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/permissions/matrix")]
[Authorize]
public sealed class PermissionMatrixController : ControllerBase
{
    private readonly IAccessRightsService _accessRights;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserActionLogger _actionLogger;

    public PermissionMatrixController(
        IAccessRightsService accessRights,
        ICurrentUserService currentUser,
        IUserActionLogger actionLogger)
    {
        _accessRights = accessRights;
        _currentUser = currentUser;
        _actionLogger = actionLogger;
    }

    [HttpGet]
    public async Task<ActionResult<PermissionMatrixDto>> Get(CancellationToken cancellationToken)
    {
        if (!IsAdministrator())
            return Forbid();

        return Ok(await _accessRights.GetMatrixAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] PermissionMatrixUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAdministrator())
            return Forbid();

        var (success, error) = await _accessRights.UpdateMatrixAsync(request, cancellationToken);
        if (success)
        {
            await _actionLogger.LogAsync(
                UserActionType.PermissionMatrixUpdate,
                entityType: "PermissionMatrix",
                description: $"Updated permission matrix ({request.Matrix.Count} objects)");
        }

        return success ? NoContent() : BadRequest(error);
    }

    private bool IsAdministrator() =>
        string.Equals(_currentUser.RoleName, nameof(UserRole.Administrator), StringComparison.Ordinal);
}
