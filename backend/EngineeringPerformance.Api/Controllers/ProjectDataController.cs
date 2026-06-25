using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/versions/{versionId:int}")]
[Authorize]
public sealed class ProjectDataController : ControllerBase
{
    private readonly IProjectDataService _data;

    public ProjectDataController(IProjectDataService data)
    {
        _data = data;
    }

    [HttpGet("physical-volumes")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.ProjectPhysicalVolumes, AccessLevel.Read)]
    public Task<PagedResult<ProjectPhysicalVolumeDto>> PhysicalVolumes(
        int projectId, int versionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? sort = null, CancellationToken cancellationToken = default) =>
        _data.GetPhysicalVolumesAsync(projectId, versionId, page, Math.Clamp(pageSize, 1, 1000), sort, cancellationToken);

    [HttpGet("labor-intensities")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.ProjectLaborIntensities, AccessLevel.Read)]
    public Task<PagedResult<ProjectLaborIntensityDto>> LaborIntensities(
        int projectId, int versionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? sort = null, CancellationToken cancellationToken = default) =>
        _data.GetLaborIntensitiesAsync(projectId, versionId, page, Math.Clamp(pageSize, 1, 1000), sort, cancellationToken);

    [HttpGet("equipment")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.ProjectEquipments, AccessLevel.Read)]
    public Task<PagedResult<ProjectEquipmentDto>> Equipment(
        int projectId, int versionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? sort = null, CancellationToken cancellationToken = default) =>
        _data.GetEquipmentAsync(projectId, versionId, page, Math.Clamp(pageSize, 1, 1000), sort, cancellationToken);

    [HttpPost("physical-volumes")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectPhysicalVolumes, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectPhysicalVolumeDto>> CreatePhysicalVolume(
        int projectId, int versionId, [FromBody] ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _data.CreatePhysicalVolumeAsync(projectId, versionId, request, cancellationToken);
            return created is null ? NotFound() : CreatedAtAction(nameof(PhysicalVolumes), new { projectId, versionId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("physical-volumes/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectPhysicalVolumes, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectPhysicalVolumeDto>> UpdatePhysicalVolume(
        int projectId, int versionId, int id, [FromBody] ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _data.UpdatePhysicalVolumeAsync(projectId, versionId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("physical-volumes/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectPhysicalVolumes, AccessLevel.Edit)]
    public async Task<IActionResult> DeletePhysicalVolume(int projectId, int versionId, int id, CancellationToken cancellationToken)
    {
        var ok = await _data.DeletePhysicalVolumeAsync(projectId, versionId, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("labor-intensities")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectLaborIntensities, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectLaborIntensityDto>> CreateLaborIntensity(
        int projectId, int versionId, [FromBody] ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _data.CreateLaborIntensityAsync(projectId, versionId, request, cancellationToken);
            return created is null ? NotFound() : CreatedAtAction(nameof(LaborIntensities), new { projectId, versionId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("labor-intensities/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectLaborIntensities, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectLaborIntensityDto>> UpdateLaborIntensity(
        int projectId, int versionId, int id, [FromBody] ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _data.UpdateLaborIntensityAsync(projectId, versionId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("labor-intensities/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectLaborIntensities, AccessLevel.Edit)]
    public async Task<IActionResult> DeleteLaborIntensity(int projectId, int versionId, int id, CancellationToken cancellationToken)
    {
        var ok = await _data.DeleteLaborIntensityAsync(projectId, versionId, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("equipment")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectEquipments, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectEquipmentDto>> CreateEquipment(
        int projectId, int versionId, [FromBody] ProjectEquipmentWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _data.CreateEquipmentAsync(projectId, versionId, request, cancellationToken);
            return created is null ? NotFound() : CreatedAtAction(nameof(Equipment), new { projectId, versionId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("equipment/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectEquipments, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectEquipmentDto>> UpdateEquipment(
        int projectId, int versionId, int id, [FromBody] ProjectEquipmentWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _data.UpdateEquipmentAsync(projectId, versionId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("equipment/{id:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectEquipments, AccessLevel.Edit)]
    public async Task<IActionResult> DeleteEquipment(int projectId, int versionId, int id, CancellationToken cancellationToken)
    {
        var ok = await _data.DeleteEquipmentAsync(projectId, versionId, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
