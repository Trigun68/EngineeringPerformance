using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Данные вкладки «Тренд» отчёта проекта: история Z, мост часов, Z от 0, S-curve.
/// Математика только через <see cref="IPerformanceCalculationEngine"/>.
/// </summary>
public sealed class ProjectTrendService(
    AppDbContext db,
    IFactAssessmentPipeline pipeline,
    IPerformanceCalculationEngine engine,
    IProjectAnalyticsCache cache,
    IZTrendAnalyticsService zTrends) : IProjectTrendService
{
    private const string CacheKey = "report-trend:v2";

    public async Task<ProjectTrendDto> GetAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<ProjectTrendDto>(projectId, CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var warnings = new List<string>();
        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Code, p.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Empty(string.Empty, string.Empty, ["Проект не найден."]);
        }

        var versions = await db.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new VersionRow(v.Id, v.Direction, v.VersionName, v.CreatedAt, v.IsPrimary))
            .ToListAsync(cancellationToken);

        if (versions.Count == 0)
        {
            var empty = Empty(project.Code, project.Name, ["Нет версий анализа."]);
            await cache.SetAsync(projectId, CacheKey, empty, cancellationToken);
            return empty;
        }

        var factId = ResolveId(versions, AnalysisDirection.Fact);
        var planId = ResolveId(versions, AnalysisDirection.Plan);

        var batch = await pipeline.EvaluateManyAsync(
            versions.Select(v => new AssessmentRequest(projectId, v.Id)).ToList(),
            cancellationToken);
        foreach (var err in batch.Errors)
            warnings.Add($"Версия {err.Key}: {err.Value}");

        var directions = await db.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);
        var directionById = directions.ToDictionary(d => d.Id);

        string FormatGroup(int id, string fallback) =>
            directionById.TryGetValue(id, out var d)
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(d, directionById)
                : fallback;

        HoursBridgeResultDto hoursBridge = HoursBridgeResultDto.Empty;
        HoursCoverageDto coverage = HoursCoverageDto.Empty;
        decimal? rawHoursDelta = null;
        if (planId is int pid && factId is int fid
            && batch.Results.TryGetValue(pid, out var planAssessment)
            && batch.Results.TryGetValue(fid, out var factAssessment))
        {
            hoursBridge = engine.ComputeHoursBridge(planAssessment.Performance, factAssessment.Performance);
            hoursBridge = new HoursBridgeResultDto(
                hoursBridge.Lines.Select(l => l with { GroupName = FormatGroup(l.GroupId, l.GroupName) }).ToList(),
                hoursBridge.TotalVolumeEffect,
                hoursBridge.TotalProductivityEffect);
            coverage = engine.ComputeHoursCoverage(planAssessment.Performance, factAssessment.Performance);
            rawHoursDelta = engine.ComputeRawHoursDelta(planAssessment.Performance, factAssessment.Performance);
        }
        else
        {
            warnings.Add("Нет пары основных версий плана и факта для моста часов.");
        }

        var zFromZero = new List<ZeroBaselineBarDto>();
        if (factId is int factVersionId && batch.Results.TryGetValue(factVersionId, out var factSnap))
        {
            var samples = factSnap.Performance
                .Where(r => !r.IsTotal && r.ParentId == null && r.WorkDirectionId >= 0)
                .Select(r => new ZeroBaselineSample(
                    FormatGroup(r.WorkDirectionId, r.WorkDirectionName),
                    r.ZScore,
                    r.SubtreeHours ?? r.FactLaborHours ?? 0m))
                .ToList();
            zFromZero = engine.BuildZeroBaselineBars(samples, topCount: 20).ToList();
        }
        else
        {
            warnings.Add("Нет основной факт-версии для водопада Z от 0.");
        }

        var zHistory = new List<ProjectTrendZPointDto>();
        var factHoursSamples = new List<HoursAtDateSample>();
        var planHoursSamples = new List<HoursAtDateSample>();
        var planPoints = new List<TrendPointSample>();
        var factPoints = new List<TrendPointSample>();
        var groupPoints = new Dictionary<int, List<TrendPointSample>>();
        var planGroupPoints = new Dictionary<int, List<TrendPointSample>>();
        var groupNames = new Dictionary<int, string>();
        var bandPoints = new List<PercentileBandPointDto>();

        foreach (var version in versions)
        {
            if (!batch.Results.TryGetValue(version.Id, out var assessment))
                continue;

            var hours = assessment.Summary.TotalFactLaborHours ?? 0m;
            var z = assessment.Summary.WeightedZScore;
            var direction = version.Direction == AnalysisDirection.Fact ? "fact" : "plan";
            zHistory.Add(new ProjectTrendZPointDto(version.CreatedAt, direction, version.VersionName, z, hours));

            var sample = new TrendPointSample(version.CreatedAt, z, hours);
            var roots = assessment.Performance
                .Where(r => !r.IsTotal && r.ParentId == null && r.WorkDirectionId >= 0)
                .ToList();

            if (version.Direction == AnalysisDirection.Fact)
            {
                factPoints.Add(sample);
                factHoursSamples.Add(new HoursAtDateSample(version.CreatedAt, hours, version.VersionName));
                var band = engine.ComputeWeightedPercentileBand(
                    roots.Where(r => r.ZScore is not null).Select(r => (
                        r.ZScore!.Value,
                        r.SubtreeHours ?? r.FactLaborHours ?? 0m)));
                if (band is not null)
                    bandPoints.Add(new PercentileBandPointDto(version.CreatedAt, band.P10, band.P90));
            }
            else if (version.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast)
            {
                planPoints.Add(sample);
                planHoursSamples.Add(new HoursAtDateSample(version.CreatedAt, hours, version.VersionName));
            }

            var targetGroups = version.Direction == AnalysisDirection.Fact ? groupPoints : planGroupPoints;
            if (version.Direction is not (AnalysisDirection.Fact or AnalysisDirection.Plan or AnalysisDirection.Forecast))
                continue;

            foreach (var row in roots)
            {
                if (!targetGroups.TryGetValue(row.WorkDirectionId, out var list))
                {
                    list = [];
                    targetGroups[row.WorkDirectionId] = list;
                    groupNames[row.WorkDirectionId] = FormatGroup(row.WorkDirectionId, row.WorkDirectionName);
                }

                list.Add(new TrendPointSample(
                    version.CreatedAt,
                    row.ZScore,
                    row.SubtreeHours ?? row.FactLaborHours ?? 0m));
            }
        }

        var sCurve = engine.ComputeCumulativeHours(factHoursSamples);
        var planSCurve = engine.ComputeCumulativeHours(planHoursSamples);
        var groups = groupPoints
            .Select(kv => new TrendGroupSample(kv.Key, groupNames.GetValueOrDefault(kv.Key, $"Группа {kv.Key}"), kv.Value))
            .ToList();
        var planGroups = planGroupPoints
            .Select(kv => new TrendGroupSample(kv.Key, groupNames.GetValueOrDefault(kv.Key, $"Группа {kv.Key}"), kv.Value))
            .ToList();
        var detail = engine.BuildTrendDetailAggregation(planPoints, factPoints, groups, planGroups: planGroups);

        var events = await LoadEventsAsync(projectId, versions, cancellationToken);
        var portfolioZ = await LoadPortfolioZAsync(versions, cancellationToken);

        var factMeta = factId is int fmeta ? versions.First(v => v.Id == fmeta) : null;
        var planMeta = planId is int pmeta ? versions.First(v => v.Id == pmeta) : null;

        var dto = new ProjectTrendDto(
            project.Code,
            project.Name,
            factMeta?.Id,
            factMeta?.VersionName,
            factMeta?.CreatedAt,
            planMeta?.Id,
            planMeta?.VersionName,
            zHistory,
            hoursBridge,
            zFromZero,
            sCurve,
            detail.FactKpi.Initial,
            detail.FactWaterfall,
            warnings.Distinct().ToList(),
            coverage,
            portfolioZ,
            bandPoints,
            events,
            planSCurve,
            rawHoursDelta,
            detail.PlanKpi.Initial,
            detail.PlanWaterfall);

        await cache.SetAsync(projectId, CacheKey, dto, cancellationToken);
        return dto;
    }

    private async Task<IReadOnlyList<TrendEventDto>> LoadEventsAsync(
        int projectId,
        IReadOnlyList<VersionRow> versions,
        CancellationToken cancellationToken)
    {
        var versionEvents = versions
            .Select(v => new TrendEventDto(
                v.CreatedAt,
                "versionCreated",
                $"{DirectionLabel(v.Direction)}: {v.VersionName}",
                v.Id))
            .ToList();

        var importEvents = await db.DataImportIterations.AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.ImportedAt)
            .Select(i => new TrendEventDto(
                i.ImportedAt,
                "import",
                $"{i.DataType} ({i.RecordCount} зап.)",
                i.ProjectAnalysisVersionId))
            .Take(20)
            .ToListAsync(cancellationToken);

        return versionEvents.Concat(importEvents).OrderBy(e => e.Timestamp).ToList();
    }

    private async Task<IReadOnlyList<ProjectTrendZPointDto>> LoadPortfolioZAsync(
        IReadOnlyList<VersionRow> versions,
        CancellationToken cancellationToken)
    {
        if (versions.Count == 0)
            return [];

        try
        {
            var portfolio = await zTrends.GetZTrendsAsync(
                new ZTrendRequest(versions[0].CreatedAt, versions[^1].CreatedAt),
                cancellationToken);
            return portfolio.Overall.Fact.Segments
                .SelectMany(s => s.Points)
                .Where(p => p.ZScore is not null)
                .Select(p => new ProjectTrendZPointDto(
                    p.Date,
                    "portfolio",
                    "Портфель",
                    p.ZScore is null ? null : (decimal)p.ZScore.Value,
                    (decimal)p.Weight))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed record VersionRow(
        int Id,
        AnalysisDirection Direction,
        string VersionName,
        DateTime CreatedAt,
        bool IsPrimary);

    private static int? ResolveId(IReadOnlyList<VersionRow> versions, AnalysisDirection direction)
    {
        var ofDirection = versions.Where(v => v.Direction == direction).ToList();
        if (ofDirection.Count == 0)
            return null;
        var primary = ofDirection.FirstOrDefault(v => v.IsPrimary);
        if (primary is not null)
            return primary.Id;
        return ofDirection.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First().Id;
    }

    private static string DirectionLabel(AnalysisDirection direction) =>
        direction switch
        {
            AnalysisDirection.Fact => "Факт",
            AnalysisDirection.Plan => "План",
            AnalysisDirection.Forecast => "Прогноз",
            _ => direction.ToString(),
        };

    private static ProjectTrendDto Empty(string code, string name, IReadOnlyList<string> warnings) =>
        new(
            code,
            name,
            null,
            null,
            null,
            null,
            null,
            [],
            HoursBridgeResultDto.Empty,
            [],
            [],
            null,
            null,
            warnings,
            HoursCoverageDto.Empty,
            [],
            [],
            [],
            [],
            null,
            null,
            null);
}
