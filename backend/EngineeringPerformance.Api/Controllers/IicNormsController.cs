using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>Алиас API норм ИИС для каскада КФО в справочнике физобъёмов.</summary>
[ApiController]
[Route("api/iic-norms")]
[Authorize]
public sealed class IicNormsController : ControllerBase
{
    private readonly IIicNormService _norms;

    public IicNormsController(IIicNormService norms)
    {
        _norms = norms;
    }

    [HttpGet]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<IicNormDto>>> List(
        [FromQuery] int? kfoId,
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        if (kfoId.HasValue)
            return Ok(await _norms.GetHistoryAsync(kfoId.Value, cancellationToken));

        return Ok(await _norms.GetAllAsync(asOf, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<IicNormDto>> Update(
        int id,
        [FromBody] IicNormWriteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _norms.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<IicNormDto>> Create(
        [FromBody] IicNormWriteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _norms.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), created);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _norms.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
