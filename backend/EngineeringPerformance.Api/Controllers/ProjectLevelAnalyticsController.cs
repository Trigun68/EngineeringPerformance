using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}")]
[Authorize]
public sealed class ProjectLevelAnalyticsController : ControllerBase
{
    private readonly IProjectCompareService _compare;
    private readonly IGroupAnalyticsService _groupAnalytics;

    public ProjectLevelAnalyticsController(
        IProjectCompareService compare,
        IGroupAnalyticsService groupAnalytics)
    {
        _compare = compare;
        _groupAnalytics = groupAnalytics;
    }

    [HttpGet("compare")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public Task<VersionCompareResultDto> Compare(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken = default) =>
        _compare.CompareAsync(projectId, versionA, versionB, cancellationToken);

    [HttpGet("group-analysis")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupAnalysis, AccessLevel.Read)]
    public Task<GroupAnalyticsPlaceholderDto> GroupAnalysis(CancellationToken cancellationToken = default) =>
        _groupAnalytics.GetGroupAnalysisAsync(cancellationToken);

    [HttpGet("group-trend")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<GroupAnalyticsPlaceholderDto> GroupTrend(CancellationToken cancellationToken = default) =>
        _groupAnalytics.GetGroupTrendAsync(cancellationToken);
}
