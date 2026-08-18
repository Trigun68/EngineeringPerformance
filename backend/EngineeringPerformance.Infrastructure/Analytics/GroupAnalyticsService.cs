using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Групповой анализ: агрегация weighted Z и часов факта по проектам одной программы или заказчика.
/// </summary>
public sealed class GroupAnalyticsService : IGroupAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private readonly IFactAssessmentPipeline _pipeline;

    public GroupAnalyticsService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility,
        IFactAssessmentPipeline pipeline)
    {
        _db = db;
        _currentUser = currentUser;
        _visibility = visibility;
        _pipeline = pipeline;
    }

    public async Task<GroupAnalysisResultDto> GetGroupAnalysisAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Read, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot read project {projectId}.");

        var anchor = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        var (criterion, groupValue, peers) = await ResolvePeerProjectsAsync(anchor, cancellationToken);

        var rows = new List<GroupAnalysisProjectRowDto>();
        var aggregator = new WeightedZAggregator();

        foreach (var project in peers.OrderBy(p => p.Code).ThenBy(p => p.Name))
        {
            if (!await _visibility.CanAccessProjectAsync(userId, project.Id, AccessLevel.Read, cancellationToken))
                continue;

            var latestFact = await _db.ProjectAnalysisVersions.AsNoTracking()
                .Where(v => v.ProjectId == project.Id && !v.IsArchived && v.Direction == AnalysisDirection.Fact)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestFact is null)
            {
                rows.Add(new GroupAnalysisProjectRowDto(
                    project.Id,
                    project.Code,
                    project.Name,
                    null,
                    null,
                    null,
                    null,
                    null));
                continue;
            }

            var assessment = await _pipeline.EvaluateAsync(
                new AssessmentRequest(project.Id, latestFact.Id),
                cancellationToken);

            var hours = assessment.Summary.TotalFactLaborHours;
            var z = assessment.Summary.WeightedZScore;
            var interpretation = PerformanceInterpretationHelper.GetInterpretation(z)?.ToString();

            rows.Add(new GroupAnalysisProjectRowDto(
                project.Id,
                project.Code,
                project.Name,
                latestFact.Id,
                latestFact.VersionName,
                hours,
                z,
                interpretation));

            aggregator.Add(z, hours);
        }

        var aggregateZ = aggregator.Result();

        return new GroupAnalysisResultDto(
            GroupCriterion: criterion,
            GroupValue: groupValue,
            Projects: rows,
            AggregateWeightedZScore: aggregateZ,
            AggregateWeightedZInterpretation: PerformanceInterpretationHelper.GetInterpretation(aggregateZ)?.ToString());
    }

    public async Task<GroupTrendInfoDto> GetGroupTrendAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Read, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot read project {projectId}.");

        var anchor = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        var (_, _, peers) = await ResolvePeerProjectsAsync(anchor, cancellationToken);
        var visibleCount = 0;
        foreach (var peer in peers)
        {
            if (await _visibility.CanAccessProjectAsync(userId, peer.Id, AccessLevel.Read, cancellationToken))
                visibleCount++;
        }

        return new GroupTrendInfoDto(
            Message: "Тренд по версиям проекта доступен на странице аналитики.",
            TrendsPagePath: $"/analytics/trends?projectId={projectId}",
            PeerProjectCount: visibleCount);
    }

    private async Task<(string Criterion, string? Value, IReadOnlyList<Project> Peers)> ResolvePeerProjectsAsync(
        Project anchor,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(anchor.Program))
        {
            var program = anchor.Program.Trim();
            var peers = await _db.Projects.AsNoTracking()
                .Where(p => !p.IsArchived && p.Program == program)
                .OrderBy(p => p.Code)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
            return ("Program", program, peers);
        }

        if (!string.IsNullOrWhiteSpace(anchor.Customer))
        {
            var customer = anchor.Customer.Trim();
            var peers = await _db.Projects.AsNoTracking()
                .Where(p => !p.IsArchived && p.Customer == customer)
                .OrderBy(p => p.Code)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
            return ("Customer", customer, peers);
        }

        return ("Single", null, new List<Project> { anchor });
    }
}
