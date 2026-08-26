using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public sealed class FactNormAnalyticsController(
    IFactNormAnalysisService analysisService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("fact-norm-analysis")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<ActionResult<FactNormAnalysisResponse>> Analyze(
        [FromBody] FactNormAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await analysisService.AnalyzeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reference-formulas")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public ActionResult<IReadOnlyList<ReferenceFormulaDto>> ListReferenceFormulas() =>
        Ok(analysisService.ListReferenceFormulas());

    [HttpGet("fact-norm-analysis/export")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime periodStart,
        [FromQuery] DateTime periodEnd,
        [FromQuery] int[]? projectIds = null,
        [FromQuery] int[]? workDirectionIds = null,
        [FromQuery] string format = "xlsx",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new FactNormAnalysisRequest(periodStart, periodEnd, projectIds, workDirectionIds);
            var (content, contentType, fileName) =
                await analysisService.ExportAsync(request, format, cancellationToken);
            return File(content, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("norms/from-suggestion")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Edit)]
    public async Task<ActionResult<IicNormDto>> CreateFromSuggestion(
        [FromBody] CreateNormFromSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var changedBy = currentUser.Username ?? "unknown";
            var created = await analysisService.CreateNormFromSuggestionAsync(
                request, changedBy, cancellationToken);
            return Ok(created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("пересеч", StringComparison.OrdinalIgnoreCase)
                                                   || ex.Message.Contains("overlap", StringComparison.OrdinalIgnoreCase)
                                                   || ex.Message.Contains("period", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }
}
