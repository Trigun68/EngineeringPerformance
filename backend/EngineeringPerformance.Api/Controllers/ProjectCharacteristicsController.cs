using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/characteristics")]
[Authorize]
public sealed class ProjectCharacteristicsController : ControllerBase
{
    private readonly IProjectCharacteristicSelectionService _selections;

    public ProjectCharacteristicsController(IProjectCharacteristicSelectionService selections)
    {
        _selections = selections;
    }

    [HttpGet]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Read)]
    [RequireProjectAccess(AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ProjectCharacteristicSelectionDto>>> Get(
        int projectId,
        CancellationToken cancellationToken)
    {
        var items = await _selections.GetByProjectAsync(projectId, cancellationToken);
        return Ok(items);
    }

    [HttpPut]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Edit)]
    [RequireProjectAccess(AccessLevel.Edit)]
    public async Task<IActionResult> Save(
        int projectId,
        [FromBody] ProjectCharacteristicSelectionsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _selections.SaveByProjectAsync(projectId, request?.Selections ?? [], cancellationToken);
        return NoContent();
    }
}
