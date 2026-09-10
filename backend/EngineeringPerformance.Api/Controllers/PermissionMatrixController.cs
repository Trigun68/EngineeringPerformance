using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>Матрица прав. Доступ — через объект Administration (только SuperAdmin), не через имя роли.</summary>
[ApiController]
[Route("api/permissions/matrix")]
[Authorize]
public sealed class PermissionMatrixController : ControllerBase
{
    private readonly IAccessRightsService _accessRights;
    private readonly IUserActionLogger _actionLogger;

    public PermissionMatrixController(
        IAccessRightsService accessRights,
        IUserActionLogger actionLogger)
    {
        _accessRights = accessRights;
        _actionLogger = actionLogger;
    }

    [HttpGet]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<PermissionMatrixDto>> Get(CancellationToken cancellationToken) =>
        Ok(await _accessRights.GetMatrixAsync(cancellationToken));

    [HttpPut]
    [HasPermission(PermissionType.Administration, AccessLevel.Edit)]
    public async Task<IActionResult> Update(
        [FromBody] PermissionMatrixUpdateRequest request,
        CancellationToken cancellationToken)
    {
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
}
