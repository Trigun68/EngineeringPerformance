using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
[HasPermission(PermissionType.UserActionLog, AccessLevel.Read)]
public sealed class AuditController : ControllerBase
{
    private readonly IUserActionLogService _userActionLog;

    public AuditController(IUserActionLogService userActionLog)
    {
        _userActionLog = userActionLog;
    }

    [HttpGet("user-actions")]
    public Task<IReadOnlyList<UserActionLogDto>> GetUserActions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? userId,
        [FromQuery] UserActionType? actionType,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 1000,
        CancellationToken cancellationToken = default)
    {
        var filter = new UserActionLogFilter(from, to, userId, actionType);
        return _userActionLog.GetAsync(filter, skip, take, includeArchived, cancellationToken);
    }

    [HttpGet("user-actions/export.xlsx")]
    public async Task<IActionResult> ExportUserActions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? userId,
        [FromQuery] UserActionType? actionType,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new UserActionLogFilter(from, to, userId, actionType);
        var bytes = await _userActionLog.ExportXlsxAsync(filter, includeArchived, cancellationToken);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Audit,
            "xlsx",
            ExportFileNameBuilder.Views.AuditLog);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost("user-actions/archive")]
    [HasPermission(PermissionType.UserActionLog, AccessLevel.Edit)]
    public async Task<ActionResult<object>> ArchiveUserActions(
        [FromBody] UserActionLogIdsRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _userActionLog.ArchiveAsync(request.Ids, cancellationToken);
        return Ok(new { archived = count });
    }

    [HttpPost("restore")]
    [HasPermission(PermissionType.UserActionLog, AccessLevel.Edit)]
    public async Task<ActionResult<object>> RestoreUserActions(
        [FromBody] UserActionLogIdsRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _userActionLog.RestoreAsync(request.Ids, cancellationToken);
        return Ok(new { restored = count });
    }
}
