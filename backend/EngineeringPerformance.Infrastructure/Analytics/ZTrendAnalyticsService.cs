using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class ZTrendAnalyticsService : IZTrendAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private readonly IFactAssessmentPipeline _pipeline;
    private readonly IZTrendEngine _engine;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly IProjectAnalyticsCache _cache;
    private readonly AnalyticsTrendsOptions _options;

    public ZTrendAnalyticsService(
        AppDbContext db,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility,
        IFactAssessmentPipeline pipeline,
        IZTrendEngine engine,
        IPerformanceCalculationEngine calculationEngine,
        IProjectAnalyticsCache cache,
        IOptions<AnalyticsTrendsOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _visibility = visibility;
        _pipeline = pipeline;
        _engine = engine;
        _calculationEngine = calculationEngine;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ZTrendFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToList();
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => accessible.Contains(p.Id) && !p.IsArchived)
            .OrderBy(p => p.Code)
            .Select(p => new ZTrendFilterProjectDto(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);

        var claimedIds = await _db.LaborIntensityIicBindings.AsNoTracking()
            .Where(b => b.IicWorkDirectionId != null)
            .Select(b => b.IicWorkDirectionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var groups = await _db.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived && (d.ParentId == null || claimedIds.Contains(d.Id)))
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new ZTrendFilterGroupDto(d.Id, d.Code ?? string.Empty, d.Name))
            .ToListAsync(cancellationToken);

        return new ZTrendFiltersDto(projects, groups, ["basisHours"]);
    }

    public async Task<ZTrendResponse> GetZTrendsAsync(ZTrendRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var weightMode = "basisHours";

        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToList();
        // List (not int[]): array.Contains binds to MemoryExtensions under C# 13 / newer SDKs and breaks EF.
        var projectIds = request.ProjectIds is { Length: > 0 }
            ? request.ProjectIds.Where(accessible.Contains).ToList()
            : accessible;

        // Портфельное поколение в ключе: мутация любого проекта сбрасывает выборку без TTL.
        var cacheKey =
            $"ztrends:v6:{userId}:{request.PeriodStart:O}:{request.PeriodEnd:O}:{weightMode}:" +
            $"{string.Join(',', projectIds.OrderBy(x => x))}:" +
            $"{string.Join(',', (request.WorkDirectionIds ?? []).OrderBy(x => x))}";

        var cached = await _cache.GetPortfolioAsync<ZTrendResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var warnings = new List<string>();
        if (projectIds.Count == 0)
        {
            var empty = EmptyResponse(warnings, "Нет доступных проектов.");
            return empty;
        }

        // Capture DateTime? locals — request.PeriodStart.Value inside the expression tree breaks EF parameter extract.
        DateTime? periodStart = request.PeriodStart;
        DateTime? periodEnd = request.PeriodEnd;

        var allVersions = await _db.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId) && !v.IsArchived)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new VersionInfo(
                v.Id,
                v.ProjectId,
                v.CreatedAt,
                v.Direction,
                v.VersionName,
                v.IsPrimary,
                v.Project!.Code,
                v.Project.Name))
            .ToListAsync(cancellationToken);

        bool InPeriod(DateTime created)
        {
            if (periodStart.HasValue && created < periodStart.Value)
                return false;
            if (periodEnd.HasValue && created > periodEnd.Value)
                return false;
            return true;
        }

        var seriesVersions = allVersions.Where(v => v.IsPrimary && InPeriod(v.CreatedAt)).ToList();
        var factPrimaryProjects = seriesVersions
            .Where(v => v.Direction == AnalysisDirection.Fact)
            .Select(v => v.ProjectId)
            .ToHashSet();
        var pairingPlans = allVersions
            .Where(v => v.IsPrimary
                && v.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast
                && factPrimaryProjects.Contains(v.ProjectId))
            .ToList();
        var sCurveFacts = allVersions
            .Where(v => v.Direction == AnalysisDirection.Fact && InPeriod(v.CreatedAt))
            .ToList();
        var sCurvePlans = allVersions
            .Where(v => v.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast && InPeriod(v.CreatedAt))
            .ToList();

        var versions = seriesVersions
            .Concat(pairingPlans)
            .Concat(sCurveFacts)
            .Concat(sCurvePlans)
            .DistinctBy(v => v.Id)
            .OrderBy(v => v.CreatedAt)
            .ToList();

        if (seriesVersions.Count == 0)
        {
            var empty = EmptyResponse(warnings, "Нет основных версий (isPrimary) в выбранном периоде.");
            await CacheAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        var seriesIds = seriesVersions.Select(v => v.Id).ToHashSet();
        var sCurveFactIds = sCurveFacts.Select(v => v.Id).ToHashSet();
        var sCurvePlanIds = sCurvePlans.Select(v => v.Id).ToHashSet();
        var versionIds = versions.Select(v => v.Id).ToList();
        var snapshots = await _db.AnalysisVersionSnapshots.AsNoTracking()
            .Where(s => versionIds.Contains(s.ProjectAnalysisVersionId))
            .Select(s => new { s.ProjectAnalysisVersionId, s.WorkDirectionId, s.NormId })
            .ToListAsync(cancellationToken);

        var normIds = snapshots.Select(s => s.NormId).Distinct().ToList();
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
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);
        var directionById = directions.ToDictionary(d => d.Id);

        var snapshotsByVersion = snapshots
            .GroupBy(s => s.ProjectAnalysisVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var overallPlan = new List<ZTrendRawPoint>();
        var overallFact = new List<ZTrendRawPoint>();
        var byGroupPlan = new Dictionary<int, List<ZTrendRawPoint>>();
        var byGroupFact = new Dictionary<int, List<ZTrendRawPoint>>();
        var planPerfByProject = new Dictionary<int, IReadOnlyList<PerformanceRowDto>>();
        var factPerfByProject = new Dictionary<int, IReadOnlyList<PerformanceRowDto>>();
        var factHoursSamples = new List<HoursAtDateSample>();
        var planHoursSamples = new List<HoursAtDateSample>();
        var factHoursByProject = new Dictionary<int, (string Name, List<HoursAtDateSample> Points)>();

        var directionFilter = request.WorkDirectionIds is { Length: > 0 }
            ? request.WorkDirectionIds.ToHashSet()
            : null;

        // Батч-оценка: данные всех версий грузятся пакетно, без N+1.
        var batch = await _pipeline.EvaluateManyAsync(
            versions.Select(v => new AssessmentRequest(v.ProjectId, v.Id)).ToList(),
            cancellationToken);

        foreach (var version in versions)
        {
            if (!batch.Results.TryGetValue(version.Id, out var assessment))
            {
                warnings.Add($"Версия {version.Id}: не удалось рассчитать показатели.");
                continue;
            }

            var versionSnaps = snapshotsByVersion.GetValueOrDefault(version.Id) ?? [];
            var regimeKey = versionSnaps.Count == 0
                ? "none"
                : string.Join('-', versionSnaps.OrderBy(s => s.WorkDirectionId).Select(s => s.NormId));

            var weight = Math.Max(0m, assessment.Summary.TotalFactLaborHours ?? 0m);
            var inSeries = seriesIds.Contains(version.Id);
            var projectLabel = string.IsNullOrWhiteSpace(version.ProjectName)
                ? version.ProjectCode
                : $"{version.ProjectCode} — {version.ProjectName}";

            if (version.Direction == AnalysisDirection.Fact)
            {
                if (inSeries)
                    factPerfByProject[version.ProjectId] = assessment.Performance;
                if (sCurveFactIds.Contains(version.Id))
                {
                    factHoursSamples.Add(new HoursAtDateSample(version.CreatedAt, weight, version.VersionName));
                    if (!factHoursByProject.TryGetValue(version.ProjectId, out var pack))
                    {
                        pack = (projectLabel, []);
                        factHoursByProject[version.ProjectId] = pack;
                    }

                    pack.Points.Add(new HoursAtDateSample(version.CreatedAt, weight, version.VersionName));
                }
            }
            else if (version.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast)
            {
                if (version.IsPrimary && (version.Direction == AnalysisDirection.Plan
                    || !planPerfByProject.ContainsKey(version.ProjectId)))
                    planPerfByProject[version.ProjectId] = assessment.Performance;
                if (sCurvePlanIds.Contains(version.Id))
                    planHoursSamples.Add(new HoursAtDateSample(version.CreatedAt, weight, version.VersionName));
            }

            if (!inSeries)
                continue;

            var clippedOverall = _engine.ClipZ(assessment.Summary.WeightedZScore, _options.ZClipMax, out var overallFlag);
            if (assessment.Summary.WeightedZScore is null)
                overallFlag = versionSnaps.Count == 0 ? "no_norm" : "no_norm";
            if (versionSnaps.Count == 0 && assessment.Summary.WeightedZScore is null)
            {
                overallFlag = "no_norm";
                warnings.Add($"Версия {version.VersionName}: нет снимка норм.");
            }

            // Detect sigma_zero from root rows
            var rootRows = PerformanceDisplayVolume.WithOwnFactHours(assessment.Performance).ToList();

            var overallPoint = new ZTrendRawPoint(
                version.CreatedAt,
                version.Id,
                version.ProjectId,
                clippedOverall,
                weight,
                regimeKey,
                null,
                regimeKey == "none" ? "Без нормы" : $"Режим норм {regimeKey}",
                0,
                "Все направления",
                overallFlag,
                ProjectCount: 1,
                ProjectCode: version.ProjectCode,
                ProjectName: version.ProjectName,
                VersionName: version.VersionName);

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
                    ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, directionById)
                    : row.WorkDirectionName;
                var normName = normId is not null
                    ? (directionById.TryGetValue(
                           normById.TryGetValue(normId.Value, out var nn) ? nn.WorkDirectionId : row.WorkDirectionId,
                           out var nd)
                        ? $"Норма {IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(nd, directionById)}"
                        : $"Норма #{normId}")
                    : "Без нормы";

                var rowWeight = Math.Max(0m, row.SubtreeHours ?? row.FactLaborHours ?? 0m);

                var groupPoint = new ZTrendRawPoint(
                    version.CreatedAt,
                    version.Id,
                    version.ProjectId,
                    z,
                    rowWeight,
                    normId?.ToString() ?? "none",
                    normId,
                    normName,
                    row.WorkDirectionId,
                    dirName,
                    flag,
                    ProjectCount: 1,
                    ProjectCode: version.ProjectCode,
                    ProjectName: version.ProjectName,
                    VersionName: version.VersionName);

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
            var name = directionById.TryGetValue(groupId, out var d)
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(d, directionById)
                : $"Направление {groupId}";
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
                    ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dn, directionById)
                    : $"Норма #{kv.Key}",
                kv.Value.WorkDirectionId)));

        var distinctWarnings = warnings.Distinct().Take(50).ToList();
        var detail = _calculationEngine.BuildTrendDetailAggregation(
            ToSamples(overall.Plan),
            ToSamples(overall.Fact),
            byGroup.Select(g => new TrendGroupSample(g.GroupId, g.GroupName, ToSamples(g.Fact))).ToList(),
            planGroups: byGroup.Select(g => new TrendGroupSample(g.GroupId, g.GroupName, ToSamples(g.Plan))).ToList());

        var disciplineBridge = BuildDisciplineBridge(planPerfByProject, factPerfByProject, directionById);
        var groupZeroBars = BuildGroupZeroBars(byGroup);
        var sCurve = _calculationEngine.ComputeCumulativeHours(factHoursSamples);
        var planSCurve = _calculationEngine.ComputeCumulativeHours(planHoursSamples);
        var projectSCurves = _calculationEngine.BuildTopProjectSCurves(
            factHoursByProject
                .Select(kv => new ProjectHoursSeriesSample(kv.Key, kv.Value.Name, kv.Value.Points))
                .ToList());
        var coverageParts = new List<HoursCoverageDto>();
        decimal rawHoursDelta = 0m;
        foreach (var projectId in factPerfByProject.Keys)
        {
            if (!planPerfByProject.TryGetValue(projectId, out var planRows))
                continue;
            coverageParts.Add(_calculationEngine.ComputeHoursCoverage(planRows, factPerfByProject[projectId]));
            rawHoursDelta += _calculationEngine.ComputeRawHoursDelta(planRows, factPerfByProject[projectId]);
        }

        var coverage = coverageParts.Count == 0
            ? HoursCoverageDto.Empty
            : _calculationEngine.CombineHoursCoverages(coverageParts);
        var movers = BuildMovers(overall.Fact);
        var band = BuildPercentileBand(overall.Fact);
        var events = await LoadEventsAsync(projectIds, seriesVersions, periodStart, periodEnd, cancellationToken);

        var response = new ZTrendResponse(
            overall,
            byGroup,
            legend,
            distinctWarnings,
            detail,
            disciplineBridge,
            groupZeroBars,
            sCurve,
            coverage,
            movers,
            band,
            events,
            planSCurve,
            projectSCurves,
            Math.Round(rawHoursDelta, 2, MidpointRounding.ToEven));
        await CacheAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private HoursBridgeResultDto BuildDisciplineBridge(
        IReadOnlyDictionary<int, IReadOnlyList<PerformanceRowDto>> planByProject,
        IReadOnlyDictionary<int, IReadOnlyList<PerformanceRowDto>> factByProject,
        IReadOnlyDictionary<int, IicWorkDirection> directionById)
    {
        var parts = new List<HoursBridgeResultDto>();
        foreach (var projectId in planByProject.Keys)
        {
            if (!factByProject.TryGetValue(projectId, out var factRows))
                continue;
            parts.Add(_calculationEngine.ComputeHoursBridge(planByProject[projectId], factRows));
        }

        var combined = _calculationEngine.CombineHoursBridges(parts);
        if (combined.Lines.Count == 0)
            return combined;

        var lines = combined.Lines
            .Select(l => l with
            {
                GroupName = directionById.TryGetValue(l.GroupId, out var d)
                    ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(d, directionById)
                    : l.GroupName,
            })
            .ToList();
        return new HoursBridgeResultDto(lines, combined.TotalVolumeEffect, combined.TotalProductivityEffect);
    }

    private IReadOnlyList<GroupZeroBaselineDto> BuildGroupZeroBars(IReadOnlyList<ZTrendGroupDto> byGroup)
    {
        var result = new List<GroupZeroBaselineDto>();
        foreach (var group in byGroup)
        {
            var points = group.Fact.Segments
                .SelectMany(s => s.Points)
                .OrderBy(p => p.Date)
                .ToList();
            var last = points.LastOrDefault(p => p.Contributions is { Count: > 0 });
            last ??= points.LastOrDefault();
            if (last?.Contributions is null || last.Contributions.Count == 0)
                continue;

            var samples = last.Contributions
                .Select(c => new ZeroBaselineSample(
                    string.IsNullOrWhiteSpace(c.ProjectName)
                        ? c.ProjectCode
                        : $"{c.ProjectCode} — {c.ProjectName}",
                    c.ZScore is null ? null : (decimal)c.ZScore.Value,
                    (decimal)c.Weight))
                .ToList();
            var bars = _calculationEngine.BuildZeroBaselineBars(samples);
            if (bars.Count == 0)
                continue;
            result.Add(new GroupZeroBaselineDto(group.GroupId, group.GroupName, bars));
        }

        return result;
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

    private IReadOnlyList<MoverRowDto> BuildMovers(ZTrendSeriesDto factSeries)
    {
        var points = factSeries.Segments.SelectMany(s => s.Points).OrderBy(p => p.Date).ToList();
        var last = points.LastOrDefault(p =>
            p.Contributions is { Count: > 0 } && p.Contributions.Any(c => c.ZScore is not null));
        last ??= points.LastOrDefault(p => p.Contributions is { Count: > 0 });
        if (last?.Contributions is null || last.Contributions.Count == 0)
            return [];

        return _calculationEngine.BuildMoverRows(
            last.Contributions
                .Select(c => new MoverSample(
                    c.ProjectId,
                    c.ProjectCode,
                    c.ProjectName,
                    c.ZScore is null ? null : (decimal)c.ZScore.Value,
                    (decimal)c.Weight))
                .ToList());
    }

    private IReadOnlyList<PercentileBandPointDto> BuildPercentileBand(ZTrendSeriesDto factSeries)
    {
        var result = new List<PercentileBandPointDto>();
        foreach (var point in factSeries.Segments.SelectMany(s => s.Points).OrderBy(p => p.Date))
        {
            if (point.Contributions is not { Count: > 0 })
                continue;
            var band = _calculationEngine.ComputeWeightedPercentileBand(
                point.Contributions
                    .Where(c => c.ZScore is not null && c.Weight > 0)
                    .Select(c => ((decimal)c.ZScore!.Value, (decimal)c.Weight)));
            if (band is not null)
                result.Add(new PercentileBandPointDto(point.Date, band.P10, band.P90));
        }

        return result;
    }

    private async Task<IReadOnlyList<TrendEventDto>> LoadEventsAsync(
        List<int> projectIds,
        IReadOnlyList<VersionInfo> seriesVersions,
        DateTime? periodStart,
        DateTime? periodEnd,
        CancellationToken cancellationToken)
    {
        var versionEvents = seriesVersions
            .OrderByDescending(v => v.CreatedAt)
            .Take(12)
            .Select(v => new TrendEventDto(
                v.CreatedAt,
                "versionCreated",
                $"{v.ProjectCode}: {v.VersionName}",
                v.Id))
            .ToList();

        var importQuery = _db.DataImportIterations.AsNoTracking()
            .Where(i => projectIds.Contains(i.ProjectId));
        if (periodStart.HasValue)
            importQuery = importQuery.Where(i => i.ImportedAt >= periodStart);
        if (periodEnd.HasValue)
            importQuery = importQuery.Where(i => i.ImportedAt <= periodEnd);

        var importTimes = await importQuery.Select(i => i.ImportedAt).ToListAsync(cancellationToken);
        var importDays = importTimes
            .GroupBy(t => t.Date)
            .Select(g => (Day: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Day)
            .Take(8)
            .ToList();

        var importEvents = importDays
            .Select(d => new TrendEventDto(d.Day, "import", $"Импорт ({d.Count})", null));

        return versionEvents.Concat(importEvents).OrderBy(e => e.Timestamp).ToList();
    }

    private sealed record VersionInfo(
        int Id,
        int ProjectId,
        DateTime CreatedAt,
        AnalysisDirection Direction,
        string VersionName,
        bool IsPrimary,
        string ProjectCode,
        string ProjectName);

    private ZTrendResponse EmptyResponse(List<string> warnings, string warning)
    {
        warnings.Add(warning);
        var emptySeries = new ZTrendSeriesDto("Plan", []);
        var emptyFact = new ZTrendSeriesDto("Fact", []);
        return new ZTrendResponse(
            new ZTrendGroupDto(0, "Общий тренд", emptySeries, emptyFact),
            [],
            [],
            warnings.Distinct().ToList(),
            TrendDetailAggregationDto.Empty);
    }

    private static IReadOnlyList<TrendPointSample> ToSamples(ZTrendSeriesDto series) =>
        series.Segments
            .SelectMany(seg => seg.Points)
            .Select(p => new TrendPointSample(
                p.Date,
                p.ZScore is null ? null : (decimal)p.ZScore.Value,
                (decimal)p.Weight))
            .ToList();

    private Task CacheAsync(string key, ZTrendResponse response, CancellationToken cancellationToken) =>
        _cache.SetPortfolioAsync(key, response, cancellationToken);
}
