using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/analytics/norm-registry")]
[Authorize]
public sealed class NormRegistryController(
    INormRegistryService registry,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("list")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<PagedResultDto<NormListItemDto>>> List(
        [FromBody] NormFilterDto filter,
        CancellationToken cancellationToken) =>
        Ok(await registry.ListAsync(filter, cancellationToken));

    [HttpGet("{normId:int}")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<NormDetailsDto>> GetDetails(
        int normId,
        CancellationToken cancellationToken)
    {
        var details = await registry.GetDetailsAsync(normId, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpPost("health-check")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<HealthSummaryDto>> HealthCheck(
        [FromBody] NormHealthCheckRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await registry.HealthCheckAsync(request?.GroupIds, cancellationToken));

    [HttpGet("health-report")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<IActionResult> ExportHealthReport(
        [FromQuery] string format = "xlsx",
        [FromQuery] int[]? groupIds = null,
        CancellationToken cancellationToken = default)
    {
        var (content, contentType, fileName) =
            await registry.ExportHealthReportAsync(format, groupIds, cancellationToken);
        return File(content, contentType, fileName);
    }

    [HttpGet("timeline")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<TimelineResponseDto>> Timeline(
        [FromQuery] bool includeHistorical = true,
        [FromQuery] bool includeFuture = true,
        CancellationToken cancellationToken = default) =>
        Ok(await registry.GetTimelineAsync(includeHistorical, includeFuture, cancellationToken));

    [HttpGet("filters")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<NormRegistryFiltersMetaDto>> Filters(
        CancellationToken cancellationToken) =>
        Ok(await registry.GetFiltersMetaAsync(cancellationToken));

    [HttpGet("{normId:int}/usage")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public async Task<ActionResult<NormUsageStatsDto>> Usage(
        int normId,
        [FromServices] INormUsageStatsService usage,
        CancellationToken cancellationToken) =>
        Ok(await usage.GetUsageAsync(normId, cancellationToken));

    [HttpPost("{normId:int}/archive")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<NormListItemDto>> Archive(
        int normId,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        try
        {
            var result = await registry.ArchiveAsync(
                normId,
                userId,
                currentUser.Username ?? "unknown",
                cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("export")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "xlsx",
        [FromQuery] int[]? groupIds = null,
        [FromQuery] string[]? statuses = null,
        [FromQuery] string[]? formulaTypes = null,
        [FromQuery] bool? onlyWithIssues = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new NormFilterDto(
            GroupIds: groupIds,
            Statuses: statuses,
            FormulaTypes: formulaTypes,
            OnlyWithIssues: onlyWithIssues,
            Search: search,
            Page: 1,
            PageSize: 200);

        var (content, contentType, fileName) =
            await registry.ExportRegistryAsync(filter, format, cancellationToken);
        return File(content, contentType, fileName);
    }
}

public sealed record NormHealthCheckRequest(int[]? GroupIds = null);
