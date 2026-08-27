using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
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
    private readonly IPerformanceCalculationEngine _engine;

    public GroupAnalyticsService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility,
        IFactAssessmentPipeline pipeline,
        IPerformanceCalculationEngine engine)
    {
        _db = db;
        _currentUser = currentUser;
        _visibility = visibility;
        _pipeline = pipeline;
        _engine = engine;
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

        // Видимость одним запросом вместо CanAccessProjectAsync на каждый проект (N+1).
        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToHashSet();
        var visiblePeers = peers
            .Where(p => accessible.Contains(p.Id))
            .OrderBy(p => p.Code).ThenBy(p => p.Name)
            .ToList();

        // Последние Fact-версии всех peer-проектов одним запросом.
        var peerIds = visiblePeers.Select(p => p.Id).ToList();
        var latestFactByProject = (await _db.ProjectAnalysisVersions.AsNoTracking()
                .Where(v => peerIds.Contains(v.ProjectId) && !v.IsArchived && v.Direction == AnalysisDirection.Fact)
                .Select(v => new { v.Id, v.ProjectId, v.CreatedAt, v.VersionName })
                .ToListAsync(cancellationToken))
            .GroupBy(v => v.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First());

        // Оценка всех версий батчем: данные грузятся пакетно, без N+1.
        var batch = await _pipeline.EvaluateManyAsync(
            latestFactByProject.Values
                .Select(v => new AssessmentRequest(v.ProjectId, v.Id))
                .ToList(),
            cancellationToken);

        var rows = new List<GroupAnalysisProjectRowDto>();
        var samples = new List<(decimal? ZScore, decimal? Weight)>();

        foreach (var project in visiblePeers)
        {
            if (!latestFactByProject.TryGetValue(project.Id, out var latestFact))
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

            if (!batch.Results.TryGetValue(latestFact.Id, out var assessment))
            {
                rows.Add(new GroupAnalysisProjectRowDto(
                    project.Id,
                    project.Code,
                    project.Name,
                    latestFact.Id,
                    latestFact.VersionName,
                    null,
                    null,
                    null));
                continue;
            }

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

            samples.Add((z, hours));
        }

        var aggregateZ = _engine.ComputeWeightedZ(samples);

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
        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToHashSet();
        var visibleCount = peers.Count(peer => accessible.Contains(peer.Id));

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
