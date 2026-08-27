using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Тренд индекса производительности (% к норме) по основным версиям проектов.
/// Данные — через IFactAssessmentPipeline (батч, без N+1), индекс — только через
/// IPerformanceCalculationEngine.ComputeProductivityIndex (SSOT).
/// </summary>
public sealed class ProductivityIndexTrendService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IProjectVisibilityService visibility,
    IFactAssessmentPipeline pipeline,
    IPerformanceCalculationEngine calculationEngine,
    IProjectAnalyticsCache cache) : IProductivityIndexTrendService
{
    public async Task<ProductivityIndexTrendResponse> GetProductivityIndexTrendAsync(
        ProductivityIndexTrendRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var accessible = (await visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToList();
        // List (not int[]): array.Contains binds to MemoryExtensions under C# 13 / newer SDKs and breaks EF.
        var projectIds = request.ProjectIds is { Length: > 0 }
            ? request.ProjectIds.Where(accessible.Contains).ToList()
            : accessible;

        var cacheKey =
            $"prod-index-trend:v2:{userId}:{request.PeriodStart:O}:{request.PeriodEnd:O}:" +
            string.Join(',', projectIds.OrderBy(x => x));

        var cached = await cache.GetPortfolioAsync<ProductivityIndexTrendResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var warnings = new List<string>();
        if (projectIds.Count == 0)
            return Empty(warnings, "Нет доступных проектов.");

        var versionsQuery = db.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId) && !v.IsArchived && v.IsPrimary);

        DateTime? periodStart = request.PeriodStart;
        DateTime? periodEnd = request.PeriodEnd;
        if (periodStart.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt >= periodStart);
        if (periodEnd.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt <= periodEnd);

        var versions = await versionsQuery
            .OrderBy(v => v.CreatedAt)
            .Select(v => new
            {
                v.Id,
                v.ProjectId,
                v.CreatedAt,
                v.Direction,
                v.VersionName,
                ProjectCode = v.Project!.Code,
                ProjectName = v.Project.Name,
            })
            .ToListAsync(cancellationToken);

        if (versions.Count == 0)
        {
            var empty = Empty(warnings, "Нет основных версий (isPrimary) в выбранном периоде.");
            await cache.SetPortfolioAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        var directions = await db.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);
        var directionById = directions.ToDictionary(d => d.Id);

        var batch = await pipeline.EvaluateManyAsync(
            versions.Select(v => new AssessmentRequest(v.ProjectId, v.Id)).ToList(),
            cancellationToken);

        var overallPlan = new List<ProductivityIndexPointDto>();
        var overallFact = new List<ProductivityIndexPointDto>();
        var byGroupPlan = new Dictionary<int, List<ProductivityIndexPointDto>>();
        var byGroupFact = new Dictionary<int, List<ProductivityIndexPointDto>>();

        foreach (var version in versions)
        {
            if (!batch.Results.TryGetValue(version.Id, out var assessment))
            {
                warnings.Add($"Версия {version.VersionName}: не удалось рассчитать показатели.");
                continue;
            }

            var rows = assessment.Performance
                .Where(r => !r.IsTotal && r.WorkDirectionId >= 0)
                .ToList();
            if (rows.Count == 0)
                continue;

            var totalHours = assessment.Summary.TotalFactLaborHours ?? 0m;
            var index = calculationEngine.ComputeProductivityIndex(rows);
            var flag = index is null ? "no_data" : "ok";
            if (index is null)
                warnings.Add($"Версия {version.VersionName}: нет строк с объёмом, нормой и часами.");

            var isFact = version.Direction == AnalysisDirection.Fact;
            var overallPoint = new ProductivityIndexPointDto(
                version.CreatedAt,
                version.Id,
                version.ProjectId,
                version.ProjectCode,
                version.ProjectName,
                version.VersionName,
                index,
                Math.Max(0m, totalHours),
                flag);
            (isFact ? overallFact : overallPlan).Add(overallPoint);

            // По корневым направлениям: подмножество строк поддерева каждого корня.
            var parentById = rows
                .Where(r => r.ParentId is not null)
                .GroupBy(r => r.WorkDirectionId)
                .ToDictionary(g => g.Key, g => g.First().ParentId!.Value);

            int FindRoot(int id)
            {
                var current = id;
                var guard = 0;
                while (parentById.TryGetValue(current, out var parent) && guard++ < 64)
                    current = parent;
                return current;
            }

            foreach (var groupRows in rows.GroupBy(r => FindRoot(r.WorkDirectionId)))
            {
                var groupIndex = calculationEngine.ComputeProductivityIndex(groupRows.ToList());
                var groupHours = groupRows.Sum(r => r.FactLaborHours ?? 0m);
                var groupPoint = new ProductivityIndexPointDto(
                    version.CreatedAt,
                    version.Id,
                    version.ProjectId,
                    version.ProjectCode,
                    version.ProjectName,
                    version.VersionName,
                    groupIndex,
                    Math.Max(0m, groupHours),
                    groupIndex is null ? "no_data" : "ok");

                var bucket = isFact ? byGroupFact : byGroupPlan;
                if (!bucket.TryGetValue(groupRows.Key, out var list))
                {
                    list = [];
                    bucket[groupRows.Key] = list;
                }

                list.Add(groupPoint);
            }
        }

        var overall = new ProductivityIndexGroupDto(0, "Все направления", overallPlan, overallFact);

        var groupIds = byGroupPlan.Keys.Union(byGroupFact.Keys).OrderBy(id => id).ToList();
        var byGroup = new List<ProductivityIndexGroupDto>();
        foreach (var groupId in groupIds)
        {
            var name = directionById.TryGetValue(groupId, out var d)
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(d, directionById)
                : $"Направление {groupId}";
            byGroup.Add(new ProductivityIndexGroupDto(
                groupId,
                name,
                byGroupPlan.GetValueOrDefault(groupId) ?? [],
                byGroupFact.GetValueOrDefault(groupId) ?? []));
        }

        var response = new ProductivityIndexTrendResponse(
            overall,
            byGroup,
            warnings.Distinct().Take(50).ToList(),
            calculationEngine.BuildTrendDetailAggregation(
                ToSamples(overall.Plan),
                ToSamples(overall.Fact),
                byGroup.Select(g => new TrendGroupSample(g.GroupId, g.GroupName, ToSamples(g.Fact))).ToList()));
        await cache.SetPortfolioAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private static ProductivityIndexTrendResponse Empty(List<string> warnings, string warning)
    {
        warnings.Add(warning);
        return new ProductivityIndexTrendResponse(
            new ProductivityIndexGroupDto(0, "Все направления", [], []),
            [],
            warnings.Distinct().ToList(),
            TrendDetailAggregationDto.Empty);
    }

    private static IReadOnlyList<TrendPointSample> ToSamples(IReadOnlyList<ProductivityIndexPointDto> points) =>
        points.Select(p => new TrendPointSample(p.Date, p.Index, p.WeightHours)).ToList();
}
