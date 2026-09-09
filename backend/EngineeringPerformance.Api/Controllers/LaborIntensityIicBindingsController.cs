using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/labor-intensities/{laborIntensityId:int}/iic-bindings")]
[Authorize]
public sealed class LaborIntensityIicBindingsController(ILaborIntensityIicBindingService bindings) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionType.LaborIntensities, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<LaborIntensityIicBindingDto>>> List(
        int laborIntensityId,
        CancellationToken cancellationToken)
    {
        return Ok(await bindings.ListAsync(laborIntensityId, cancellationToken));
    }

    [HttpPost]
    [HasPermission(PermissionType.LaborIntensities, AccessLevel.Edit)]
    public async Task<ActionResult<LaborIntensityIicBindingDto>> Create(
        int laborIntensityId,
        [FromBody] LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await bindings.CreateAsync(laborIntensityId, request, cancellationToken);
        return created is null ? NotFound() : Ok(created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionType.LaborIntensities, AccessLevel.Edit)]
    public async Task<ActionResult<LaborIntensityIicBindingDto>> Update(
        int laborIntensityId,
        int id,
        [FromBody] LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await bindings.UpdateAsync(id, request, cancellationToken);
        return updated is null || updated.LaborIntensityId != laborIntensityId ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionType.LaborIntensities, AccessLevel.Edit)]
    public async Task<IActionResult> Delete(
        int laborIntensityId,
        int id,
        CancellationToken cancellationToken)
    {
        var existing = await bindings.ListAsync(laborIntensityId, cancellationToken);
        if (existing.All(x => x.Id != id))
            return NotFound();

        var ok = await bindings.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
