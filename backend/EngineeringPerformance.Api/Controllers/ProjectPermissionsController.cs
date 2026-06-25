using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/permissions")]
[Authorize]
[HasPermission(PermissionType.Projects, AccessLevel.Edit)]
public sealed class ProjectPermissionsController : ControllerBase
{
    private readonly IProjectVisibilityService _visibility;

    public ProjectPermissionsController(IProjectVisibilityService visibility)
    {
        _visibility = visibility;
    }

    [HttpGet]
    [RequireProjectAccess(AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ProjectPermissionDto>>> Get(int projectId, CancellationToken cancellationToken)
    {
        return Ok(await _visibility.GetProjectPermissionsAsync(projectId, cancellationToken));
    }

    [HttpPut]
    [RequireProjectAccess(AccessLevel.Edit)]
    public async Task<IActionResult> BulkUpsert(int projectId, [FromBody] ProjectPermissionsBulkRequest request, CancellationToken cancellationToken)
    {
        await _visibility.SaveProjectPermissionsAsync(projectId, request.Participants, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    public async Task<IActionResult> Remove(int projectId, int userId, CancellationToken cancellationToken)
    {
        var ok = await _visibility.RemoveProjectPermissionAsync(projectId, userId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
