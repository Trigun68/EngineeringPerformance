using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Export;
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
    private readonly IWorkCostService _workCosts;
    private readonly IDirectoryExportService _directoryExport;

    public RegistersController(
        IIicNormService norms,
        IWorkCostService workCosts,
        IDirectoryExportService directoryExport)
    {
        _norms = norms;
        _workCosts = workCosts;
        _directoryExport = directoryExport;
    }

    [HttpPost("iic-work-direction-norms/export")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public IActionResult ExportExcelPost([FromBody] DirectoryExportRequest request)
    {
        var bytes = _directoryExport.ExportToExcel(request);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Directory,
            "xlsx",
            ExportFileNameBuilder.Views.IicNorms);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Directory,
            "xlsx",
            ExportFileNameBuilder.Views.IicNorms);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    [HttpGet("work-costs")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<WorkCostRowDto>>> ListWorkCosts(
        [FromQuery] DateTime? asOf,
        [FromQuery] DateTime? effectiveFrom,
        CancellationToken cancellationToken)
    {
        if (effectiveFrom.HasValue)
            return Ok(await _workCosts.GetByEffectiveFromAsync(effectiveFrom.Value, cancellationToken));

        return Ok(await _workCosts.GetAsOfAsync(asOf, cancellationToken));
    }

    [HttpGet("work-costs/periods")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<WorkCostPeriodDto>>> ListWorkCostPeriods(
        CancellationToken cancellationToken)
    {
        return Ok(await _workCosts.ListPeriodsAsync(cancellationToken));
    }

    [HttpDelete("work-costs/periods")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Edit)]
    public async Task<ActionResult<WorkCostPeriodDeleteResult>> DeleteWorkCostPeriod(
        [FromQuery] DateTime? effectiveFrom,
        CancellationToken cancellationToken)
    {
        if (!effectiveFrom.HasValue)
            return BadRequest("Укажите дату «Действует с».");

        var result = await _workCosts.DeletePeriodAsync(effectiveFrom.Value, cancellationToken);
        return result.DeletedCount == 0 ? NotFound() : Ok(result);
    }

    [HttpPost("work-costs/batch")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Edit)]
    public async Task<ActionResult<WorkCostBatchResult>> SaveWorkCosts(
        [FromBody] WorkCostBatchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _workCosts.SaveBatchAsync(request, cancellationToken));
    }

    [HttpPost("work-costs/export")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Read)]
    public IActionResult ExportWorkCostsExcelPost([FromBody] DirectoryExportRequest request)
    {
        var bytes = _directoryExport.ExportToExcel(request);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Directory,
            "xlsx",
            ExportFileNameBuilder.Views.WorkCosts);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("work-costs/export.xlsx")]
    [HasPermission(PermissionType.EngineeringWorkCosts, AccessLevel.Read)]
    public async Task<IActionResult> ExportWorkCostsExcel(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var items = await _workCosts.GetAsOfAsync(asOf, cancellationToken);
        var columns = new List<ReferenceExcelExporter.ColumnDef>
        {
            new("Группа", x => ((WorkCostRowDto)x).WorkDirectionName),
            new("Стоимость, ₽/ч", x => ((WorkCostRowDto)x).RatePerHour?.ToString("0.####")),
            new("Действует с", x => ((WorkCostRowDto)x).EffectiveFrom?.ToString("yyyy-MM-dd")),
        };

        var bytes = ReferenceExcelExporter.Export("work-costs", columns, items.Cast<object>());
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Directory,
            "xlsx",
            ExportFileNameBuilder.Views.WorkCosts);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
