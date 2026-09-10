using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/versions")]
[Authorize]
[HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Read)]
public sealed class ProjectVersionsController : ControllerBase
{
    private readonly IProjectVersionService _versions;
    private readonly IProjectVersionPlanningParameterService _planningParameters;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;
    private readonly IAggregatedLaborIntensitiesService _aggregation;
    private readonly IProjectVersionReadinessService _readiness;

    public ProjectVersionsController(
        IProjectVersionService versions,
        IProjectVersionPlanningParameterService planningParameters,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
        IAggregatedLaborIntensitiesService aggregation,
        IProjectVersionReadinessService readiness)
    {
        _versions = versions;
        _planningParameters = planningParameters;
        _recalcOrchestrator = recalcOrchestrator;
        _aggregation = aggregation;
        _readiness = readiness;
    }

    [HttpGet]
    [RequireProjectAccess]
    public async Task<ActionResult<IReadOnlyList<ProjectVersionDto>>> List(int projectId, CancellationToken cancellationToken)
    {
        return Ok(await _versions.ListAsync(projectId, cancellationToken));
    }

    [HttpGet("{versionId:int}")]
    [RequireProjectAccess]
    public async Task<ActionResult<ProjectVersionDto>> Get(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var item = await _versions.GetByIdAsync(projectId, versionId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Проверка полноты версии: какие блоки данных ещё не заполнены.</summary>
    [HttpGet("{versionId:int}/readiness")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<ProjectVersionReadinessDto>> GetReadiness(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var result = await _readiness.GetAsync(projectId, versionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectVersionDto>> Create(int projectId, [FromBody] ProjectVersionWriteRequest request, CancellationToken cancellationToken)
    {
        var created = await _versions.CreateAsync(projectId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { projectId, versionId = created.Id }, created);
    }

    [HttpPut("{versionId:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectVersionDto>> Update(int projectId, int versionId, [FromBody] ProjectVersionWriteRequest request, CancellationToken cancellationToken)
    {
        var updated = await _versions.UpdateAsync(projectId, versionId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{versionId:int}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<IActionResult> Archive(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var ok = await _versions.ArchiveAsync(projectId, versionId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{versionId:int}/restore")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectVersionDto>> Restore(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var restored = await _versions.UnarchiveAsync(projectId, versionId, cancellationToken);
        return restored is null ? NotFound() : Ok(restored);
    }

    [HttpPost("{versionId:int}/set-primary")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectVersionDto>> SetPrimary(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var updated = await _versions.SetPrimaryAsync(projectId, versionId, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("{versionId:int}/copy")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectVersionDto>> Copy(
        int projectId,
        int versionId,
        [FromBody] CopyVersionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var copy = await _versions.CopyAsync(
                projectId,
                versionId,
                request.NewVersionName,
                request.Description,
                request.TargetDirection,
                cancellationToken);
            return Ok(copy);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{versionId:int}/planning-parameters")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ProjectVersionPlanningParameterDto>>> ListPlanningParameters(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _planningParameters.ListAsync(projectId, versionId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{versionId:int}/planning-parameters")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.Planning, AccessLevel.Edit)]
    public async Task<IActionResult> SavePlanningParameters(
        int projectId,
        int versionId,
        [FromBody] SaveProjectVersionPlanningParametersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _planningParameters.SaveAsync(projectId, versionId, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{versionId:int}/recalculate")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.Planning, AccessLevel.Edit)]
    public async Task<IActionResult> RecalculateLaborIntensities(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var updated = await _recalcOrchestrator.RecalculateVersionAsync(
            projectId, versionId, "button", cancellationToken);
        return Ok(new { updatedRows = updated });
    }

    [HttpGet("{versionId:int}/calculation-log")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.ProjectAnalysisVersions, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<VersionCalculationLogDto>>> GetCalculationLog(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _recalcOrchestrator.ListLogsAsync(projectId, versionId, cancellationToken));
    }

    [HttpGet("{versionId:int}/aggregated-labor-intensities")]
    [RequireProjectAccess]
    public async Task<ActionResult<AggregatedLaborIntensitiesDto>> GetAggregatedLaborIntensities(
        int projectId,
        int versionId,
        [FromQuery] AggregationLevel level = AggregationLevel.Total,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _aggregation.GetAsync(projectId, versionId, level, cancellationToken));
    }
}
