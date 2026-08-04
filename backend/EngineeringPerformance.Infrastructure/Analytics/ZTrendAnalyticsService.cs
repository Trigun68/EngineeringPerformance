using System.Text.Json;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class ZTrendAnalyticsService : IZTrendAnalyticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private readonly IFactAssessmentPipeline _pipeline;
    private readonly IZTrendEngine _engine;
    private readonly IDistributedCache _cache;
    private readonly AnalyticsTrendsOptions _options;

    public ZTrendAnalyticsService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility,
        IFactAssessmentPipeline pipeline,
        IZTrendEngine engine,
        IDistributedCache cache,
        IOptions<AnalyticsTrendsOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _visibility = visibility;
        _pipeline = pipeline;
        _engine = engine;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ZTrendFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var accessible = await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken);
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => accessible.Contains(p.Id) && !p.IsArchived)
            .OrderBy(p => p.Code)
            .Select(p => new ZTrendFilterProjectDto(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);

        var groups = await _db.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived && d.ParentId == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new ZTrendFilterGroupDto(d.Id, d.Code ?? string.Empty, d.Name))
            .ToListAsync(cancellationToken);

        return new ZTrendFiltersDto(projects, groups, ["basisHours", "uniform"]);
    }

    public async Task<ZTrendResponse> GetZTrendsAsync(ZTrendRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var weightMode = string.IsNullOrWhiteSpace(request.WeightMode)
            ? _options.WeightMode
            : request.WeightMode;

        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToHashSet();
        var projectIds = request.ProjectIds is { Length: > 0 }
            ? request.ProjectIds.Where(accessible.Contains).ToArray()
            : accessible.ToArray();

        var cacheKey =
            $"ztrends:{userId}:{request.PeriodStart:O}:{request.PeriodEnd:O}:{weightMode}:" +
            $"{string.Join(',', projectIds.OrderBy(x => x))}:" +
            $"{string.Join(',', (request.WorkDirectionIds ?? []).OrderBy(x => x))}";

        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var hit = JsonSerializer.Deserialize<ZTrendResponse>(cached, JsonOptions);
            if (hit is not null)
                return hit;
        }

        var warnings = new List<string>();
        if (projectIds.Length == 0)
        {
            var empty = EmptyResponse(warnings, "Нет доступных проектов.");
            return empty;
        }

        var versionsQuery = _db.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId) && !v.IsArchived && v.IsPrimary);

        if (request.PeriodStart.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt >= request.PeriodStart.Value);
        if (request.PeriodEnd.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt <= request.PeriodEnd.Value);

        var versions = await versionsQuery
            .OrderBy(v => v.CreatedAt)
            .Select(v => new
            {
                v.Id,
                v.ProjectId,
                v.CreatedAt,
                v.Direction,
                v.VersionName,
            })
            .ToListAsync(cancellationToken);

        if (versions.Count == 0)
        {
            var empty = EmptyResponse(warnings, "Нет основных версий (isPrimary) в выбранном периоде.");
            await CacheAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        var versionIds = versions.Select(v => v.Id).ToArray();
        var snapshots = await _db.AnalysisVersionSnapshots.AsNoTracking()
            .Where(s => versionIds.Contains(s.ProjectAnalysisVersionId))
            .Select(s => new { s.ProjectAnalysisVersionId, s.WorkDirectionId, s.NormId })
            .ToListAsync(cancellationToken);

        var normIds = snapshots.Select(s => s.NormId).Distinct().ToArray();
        var norms = await _db.IicWorkDirectionNorms.AsNoTracking()
            .Where(n => normIds.Contains(n.Id))
            .Select(n => new
            {
                n.Id,
                n.WorkDirectionId,
                n.EffectiveFrom,
                n.EffectiveTo,
            })
            .ToListAsync(cancellationToken);
        var normById = norms.ToDictionary(n => n.Id);

        var directions = await _db.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived)
            .Select(d => new { d.Id, d.Name, d.Code, d.ParentId })
            .ToListAsync(cancellationToken);
        var directionById = directions.ToDictionary(d => d.Id);

        var snapshotsByVersion = snapshots
            .GroupBy(s => s.ProjectAnalysisVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var overallPlan = new List<ZTrendRawPoint>();
        var overallFact = new List<ZTrendRawPoint>();
        var byGroupPlan = new Dictionary<int, List<ZTrendRawPoint>>();
        var byGroupFact = new Dictionary<int, List<ZTrendRawPoint>>();

        var directionFilter = request.WorkDirectionIds is { Length: > 0 }
            ? request.WorkDirectionIds.ToHashSet()
            : null;

        foreach (var version in versions)
        {
            AssessmentResultDto assessment;
            try
            {
                assessment = await _pipeline.EvaluateAsync(
                    new AssessmentRequest(version.ProjectId, version.Id),
                    cancellationToken);
            }
            catch
            {
                warnings.Add($"Версия {version.Id}: не удалось рассчитать показатели.");
                continue;
            }

            var versionSnaps = snapshotsByVersion.GetValueOrDefault(version.Id) ?? [];
            var regimeKey = versionSnaps.Count == 0
                ? "none"
                : string.Join('-', versionSnaps.OrderBy(s => s.WorkDirectionId).Select(s => s.NormId));

            var weight = string.Equals(weightMode, "uniform", StringComparison.OrdinalIgnoreCase)
                ? 1m
                : Math.Max(0m, assessment.Summary.TotalFactLaborHours ?? 0m);

            var clippedOverall = _engine.ClipZ(assessment.Summary.WeightedZScore, _options.ZClipMax, out var overallFlag);
            if (assessment.Summary.WeightedZScore is null)
                overallFlag = versionSnaps.Count == 0 ? "no_norm" : "no_norm";
            if (versionSnaps.Count == 0 && assessment.Summary.WeightedZScore is null)
            {
                overallFlag = "no_norm";
                warnings.Add($"Версия {version.VersionName}: нет снимка норм.");
            }

            // Detect sigma_zero from root rows
            var rootRows = assessment.Performance
                .Where(r => !r.IsTotal && r.ParentId == null && r.WorkDirectionId >= 0)
                .ToList();

            var overallPoint = new ZTrendRawPoint(
                version.CreatedAt,
                version.Id,
                version.ProjectId,
                clippedOverall,
                weight <= 0 && !string.Equals(weightMode, "uniform", StringComparison.OrdinalIgnoreCase)
                    ? 1m
                    : weight,
                regimeKey,
                null,
                regimeKey == "none" ? "Без нормы" : $"Режим норм {regimeKey}",
                0,
                "Все направления",
                overallFlag);

            if (version.Direction == AnalysisDirection.Fact)
                overallFact.Add(overallPoint);
            else if (version.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast)
                overallPlan.Add(overallPoint);

            foreach (var row in rootRows)
            {
                if (directionFilter is not null && !directionFilter.Contains(row.WorkDirectionId))
                    continue;

                var snap = versionSnaps.FirstOrDefault(s => s.WorkDirectionId == row.WorkDirectionId);
                int? normId = snap?.NormId;
                var flag = "ok";
                decimal? z = row.ZScore;

                if (normId is null)
                {
                    flag = "no_norm";
                    z = null;
                }
                else if (row.NormStdDev is null || (double)row.NormStdDev.Value < _options.MinSigma)
                {
                    flag = "sigma_zero";
                    z = null;
                    warnings.Add($"Версия {version.VersionName}, направление {row.WorkDirectionName}: σ≈0.");
                }
                else
                {
                    z = _engine.ClipZ(z, _options.ZClipMax, out flag);
                }

                var dirName = directionById.TryGetValue(row.WorkDirectionId, out var dir)
                    ? dir.Name
                    : row.WorkDirectionName;
                var normName = normId is not null
                    ? (directionById.TryGetValue(
                           normById.TryGetValue(normId.Value, out var nn) ? nn.WorkDirectionId : row.WorkDirectionId,
                           out var nd)
                        ? $"Норма {nd.Name}"
                        : $"Норма #{normId}")
                    : "Без нормы";

                var rowWeight = string.Equals(weightMode, "uniform", StringComparison.OrdinalIgnoreCase)
                    ? 1m
                    : Math.Max(0m, row.SubtreeHours ?? row.FactLaborHours ?? 0m);

                var groupPoint = new ZTrendRawPoint(
                    version.CreatedAt,
                    version.Id,
                    version.ProjectId,
                    z,
                    rowWeight <= 0 ? 1m : rowWeight,
                    normId?.ToString() ?? "none",
                    normId,
                    normName,
                    row.WorkDirectionId,
                    dirName,
                    flag);

                var bucket = version.Direction == AnalysisDirection.Fact ? byGroupFact : byGroupPlan;
                if (version.Direction is not (AnalysisDirection.Fact or AnalysisDirection.Plan or AnalysisDirection.Forecast))
                    continue;

                if (!bucket.TryGetValue(row.WorkDirectionId, out var list))
                {
                    list = [];
                    bucket[row.WorkDirectionId] = list;
                }

                list.Add(groupPoint);
            }
        }

        if (_options.AggregateSameDay)
        {
            overallPlan = _engine.AggregateSameDay(overallPlan, weightMode).ToList();
            overallFact = _engine.AggregateSameDay(overallFact, weightMode).ToList();
            foreach (var key in byGroupPlan.Keys.ToList())
                byGroupPlan[key] = _engine.AggregateSameDay(byGroupPlan[key], weightMode).ToList();
            foreach (var key in byGroupFact.Keys.ToList())
                byGroupFact[key] = _engine.AggregateSameDay(byGroupFact[key], weightMode).ToList();
        }

        var colorMap = BuildColorMap(overallPlan.Concat(overallFact).Concat(byGroupPlan.Values.SelectMany(x => x)).Concat(byGroupFact.Values.SelectMany(x => x)).ToList());

        var overall = new ZTrendGroupDto(
            0,
            "Общий тренд",
            _engine.BuildSeries(overallPlan, "Plan", _options, colorMap),
            _engine.BuildSeries(overallFact, "Fact", _options, colorMap));

        var groupIds = byGroupPlan.Keys.Union(byGroupFact.Keys).OrderBy(id => id).ToList();
        var byGroup = new List<ZTrendGroupDto>();
        foreach (var groupId in groupIds)
        {
            var name = directionById.TryGetValue(groupId, out var d) ? d.Name : $"Направление {groupId}";
            byGroup.Add(new ZTrendGroupDto(
                groupId,
                name,
                _engine.BuildSeries(byGroupPlan.GetValueOrDefault(groupId) ?? [], "Plan", _options, colorMap),
                _engine.BuildSeries(byGroupFact.GetValueOrDefault(groupId) ?? [], "Fact", _options, colorMap)));
        }

        var legend = BuildLegend(overall, byGroup, colorMap, normById.ToDictionary(
            kv => kv.Key,
            kv => (
                kv.Value.EffectiveFrom,
                kv.Value.EffectiveTo,
                directionById.TryGetValue(kv.Value.WorkDirectionId, out var dn)
                    ? dn.Name
                    : $"Норма #{kv.Key}",
                kv.Value.WorkDirectionId)));

        var distinctWarnings = warnings.Distinct().Take(50).ToList();
        var response = new ZTrendResponse(overall, byGroup, legend, distinctWarnings);
        await CacheAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private Dictionary<string, string> BuildColorMap(IReadOnlyList<ZTrendRawPoint> allPoints)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var byGroup = allPoints
            .GroupBy(p => p.GroupId ?? 0)
            .ToList();

        var paletteIndex = 0;
        foreach (var group in byGroup.OrderBy(g => g.Key))
        {
            var keys = group
                .Select(p => p.SegmentKey)
                .Distinct()
                .OrderBy(k => k)
                .ToList();
            var shade = 0;
            foreach (var key in keys)
            {
                if (map.ContainsKey(key))
                    continue;
                map[key] = _engine.ColorForSegment(paletteIndex, shade, _options.LightenStep);
                shade++;
            }

            paletteIndex++;
        }

        return map;
    }

    private static IReadOnlyList<ZTrendLegendDto> BuildLegend(
        ZTrendGroupDto overall,
        IReadOnlyList<ZTrendGroupDto> byGroup,
        IReadOnlyDictionary<string, string> colorMap,
        IReadOnlyDictionary<int, (DateTime EffectiveFrom, DateTime? EffectiveTo, string Name, int WorkDirectionId)> norms)
    {
        var items = new Dictionary<string, ZTrendLegendDto>(StringComparer.Ordinal);

        void AddFromSeries(ZTrendSeriesDto series)
        {
            foreach (var seg in series.Segments)
            {
                if (items.ContainsKey(seg.SegmentKey))
                    continue;
                DateTime from = seg.EffectiveFrom;
                DateTime? to = seg.EffectiveTo;
                var name = seg.NormName;
                if (seg.NormId is int nid && norms.TryGetValue(nid, out var n))
                {
                    from = n.EffectiveFrom;
                    to = n.EffectiveTo;
                    name = n.Name;
                }

                items[seg.SegmentKey] = new ZTrendLegendDto(
                    seg.NormId,
                    name,
                    seg.NormGroupId,
                    seg.NormGroupName,
                    colorMap.TryGetValue(seg.SegmentKey, out var c) ? c : seg.Color,
                    seg.SegmentKey,
                    from,
                    to);
            }
        }

        AddFromSeries(overall.Plan);
        AddFromSeries(overall.Fact);
        foreach (var g in byGroup)
        {
            AddFromSeries(g.Plan);
            AddFromSeries(g.Fact);
        }

        return items.Values.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.NormName).ToList();
    }

    private ZTrendResponse EmptyResponse(List<string> warnings, string warning)
    {
        warnings.Add(warning);
        var emptySeries = new ZTrendSeriesDto("Plan", []);
        var emptyFact = new ZTrendSeriesDto("Fact", []);
        return new ZTrendResponse(
            new ZTrendGroupDto(0, "Общий тренд", emptySeries, emptyFact),
            [],
            [],
            warnings.Distinct().ToList());
    }

    private async Task CacheAsync(string key, ZTrendResponse response, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromMinutes(Math.Max(1, _options.CacheTtlMinutes));
        var json = JsonSerializer.Serialize(response, JsonOptions);
        await _cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }
}
