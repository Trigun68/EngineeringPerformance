using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/registers")]
[Authorize]
public sealed class RegistersController : ControllerBase
{
    private readonly IIicNormService _norms;

    public RegistersController(IIicNormService norms)
    {
        _norms = norms;
    }

    [HttpGet("iic-work-direction-norms/export.xlsx")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var items = await _norms.GetAllAsync(asOf: null, cancellationToken);
        var columns = new List<ReferenceExcelExporter.ColumnDef>
        {
            new("ID", x => ((IicNormDto)x).Id.ToString()),
            new("Направление", x => ((IicNormDto)x).WorkDirectionName),
            new("Performance", x => ((IicNormDto)x).Performance?.ToString()),
            new("Estimate", x => ((IicNormDto)x).Estimate?.ToString()),
            new("σ", x => ((IicNormDto)x).StandardDeviation?.ToString()),
            new("UnitId", x => ((IicNormDto)x).UnitId?.ToString()),
            new("С", x => ((IicNormDto)x).EffectiveFrom.ToString("yyyy-MM-dd")),
            new("По", x => ((IicNormDto)x).EffectiveTo?.ToString("yyyy-MM-dd")),
        };

        var bytes = ReferenceExcelExporter.Export("iic-work-direction-norms", columns, items.Cast<object>());
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "iic-work-direction-norms.xlsx");
    }

    [HttpGet("iic-work-direction-norms")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<IicNormDto>>> List(
        [FromQuery] DateTime? asOf,
        [FromQuery] int? workDirectionId,
        CancellationToken cancellationToken)
    {
        if (workDirectionId.HasValue)
            return Ok(await _norms.GetHistoryAsync(workDirectionId.Value, cancellationToken));

        return Ok(await _norms.GetAllAsync(asOf, cancellationToken));
    }

    [HttpGet("iic-work-direction-norms/{id:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<IicNormDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _norms.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("iic-work-direction-norms")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<IicNormDto>> Create([FromBody] IicNormWriteRequest request, CancellationToken cancellationToken)
    {
        var created = await _norms.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("iic-work-direction-norms/{id:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<IicNormDto>> Update(int id, [FromBody] IicNormWriteRequest request, CancellationToken cancellationToken)
    {
        var updated = await _norms.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("iic-work-direction-norms/{id:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _norms.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
