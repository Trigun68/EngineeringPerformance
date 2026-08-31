using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Индекс N(V*): часы только из <see cref="IPerformanceCalculationEngine.ComputeForecastHours"/>
/// (PlanningZParameters.Default, ZBase = 0). Индекс и декомпозиция — отношения этих часов.
/// </summary>
public sealed class ProductivityTrendAnalyticsService : IProductivityTrendAnalyticsService
{
    private const string SnapshotPointerWarning =
        "Индекс строится по записям регистра норм. Снимки версий хранят только NormId, коэффициенты не замораживаются.";

    private static readonly IReadOnlyDictionary<FormulaType, string> FormulaColors =
        new Dictionary<FormulaType, string>
        {
            [FormulaType.Linear] = "#1976d2",
            [FormulaType.Logarithmic] = "#c62828",
            [FormulaType.ShiftedLog] = "#ef6c00",
            [FormulaType.Wright] = "#6a1b9a",
            [FormulaType.Power] = "#00897b",
            [FormulaType.ExponentialDecay] = "#4527a0",
            [FormulaType.PiecewiseLinear] = "#5d4037",
            [FormulaType.Polynomial] = "#283593",
            [FormulaType.Constant] = "#546e7a",
            [FormulaType.Hyperbolic] = "#ad1457",
            [FormulaType.Exponential] = "#2e7d32",
            [FormulaType.KimCurve] = "#00838f",
        };

    private readonly IAnalyticsDbContextFactory _analytics;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private readonly IPerformanceCalculationEngine _engine;
    private readonly IProjectAnalyticsCache _cache;
    private readonly AnalyticsTrendsOptions _options;

    public ProductivityTrendAnalyticsService(
        IAnalyticsDbContextFactory analytics,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility,
        IPerformanceCalculationEngine engine,
        IProjectAnalyticsCache cache,
        IOptions<AnalyticsTrendsOptions> options)
    {
        _analytics = analytics;
        _currentUser = currentUser;
        _visibility = visibility;
        _engine = engine;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ProductivityTrendResponse> GetProductivityTrendAsync(
        ProductivityTrendRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var volumeMode = NormalizeVolumeMode(request.VolumeMode);
        var accessible = (await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken)).ToList();
        var projectIds = request.ProjectIds is { Length: > 0 }
            ? request.ProjectIds.Where(accessible.Contains).ToList()
            : accessible;

        var cacheKey =
            $"prod-trend:{userId}:{request.PeriodStart:O}:{request.PeriodEnd:O}:{volumeMode}:" +
            $"{string.Join(',', projectIds.OrderBy(x => x))}:" +
            $"{string.Join(',', (request.WorkDirectionIds ?? []).OrderBy(x => x))}";

        // Портфельное поколение в ключе: мутация любого проекта сбрасывает выборку без TTL.
        var cached = await _cache.GetPortfolioAsync<ProductivityTrendResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var warnings = new List<string> { SnapshotPointerWarning };
        if (projectIds.Count == 0)
        {
            var empty = EmptyResponse(volumeMode, warnings, "Нет доступных проектов.");
            await CacheAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        await using var db = await _analytics.CreateDbContextAsync(cancellationToken);

        DateTime? periodStart = request.PeriodStart;
        DateTime? periodEnd = request.PeriodEnd;

        var directionFilter = (request.WorkDirectionIds ?? []).Where(id => id > 0).Distinct().ToList();

        var normsQuery = db.IicWorkDirectionNorms.AsNoTracking().AsQueryable();
        if (directionFilter.Count > 0)
            normsQuery = normsQuery.Where(n => directionFilter.Contains(n.WorkDirectionId));
        if (periodStart.HasValue)
        {
            var start = periodStart.Value;
            normsQuery = normsQuery.Where(n => n.EffectiveTo == null || n.EffectiveTo >= start);
        }

        if (periodEnd.HasValue)
        {
            var end = periodEnd.Value;
            normsQuery = normsQuery.Where(n => n.EffectiveFrom <= end);
        }

        var norms = await normsQuery.ToListAsync(cancellationToken);
        if (norms.Count == 0)
        {
            var empty = EmptyResponse(volumeMode, warnings, "Нет норм IIC, пересекающих выбранный период.");
            await CacheAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        var directions = await db.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync(cancellationToken);
        var directionById = directions.ToDictionary(d => d.Id);

        var versionsQuery = db.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId)
                        && !v.IsArchived
                        && v.IsPrimary
                        && v.Direction == AnalysisDirection.Fact);

        if (periodStart.HasValue)
        {
            var start = periodStart.Value;
            versionsQuery = versionsQuery.Where(v => v.CreatedAt >= start);
        }

        if (periodEnd.HasValue)
        {
            var end = periodEnd.Value;
            versionsQuery = versionsQuery.Where(v => v.CreatedAt <= end);
        }

        var versions = await versionsQuery
            .Select(v => new VersionRow(v.Id, v.ProjectId, v.CreatedAt, v.Project!.Code))
            .ToListAsync(cancellationToken);

        var versionIds = versions.Select(v => v.Id).ToList();
        var versionById = versions.ToDictionary(v => v.Id);

        var volumeSamples = await LoadVolumeSamplesAsync(db, versionIds, versionById, cancellationToken);

        var normsByDirection = norms
            .GroupBy(n => n.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.EffectiveFrom).ToList());

        var timeline = BuildTimeline(periodStart, periodEnd, norms);
        var byGroup = new List<ProductivityTrendGroupDto>();
        var transitions = new List<ProductivityTrendTransitionDto>();
        var counterfactuals = new List<ProductivityTrendCounterfactualDto>();
        var usedTypes = new HashSet<FormulaType>();
        var overallHours = new Dictionary<DateTime, decimal>();

        foreach (var (directionId, directionNorms) in normsByDirection.OrderBy(kv => kv.Key))
        {
            if (!directionById.TryGetValue(directionId, out var direction))
                continue;

            var groupName = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(direction, directionById);
            var classifierIds = SelfAndDescendants(directionId, directionById);
            var samples = volumeSamples
                .Where(s => classifierIds.Contains(s.ClassifierId))
                .ToList();
            var projectVolumes = ProjectTotals(samples);

            var vStar = ResolveVStar(projectVolumes, volumeMode, directionNorms, out var vStarSource);
            if (vStarSource == "fallback_bounds")
                warnings.Add($"Группа {groupName}: нет фактических объёмов оценки, V* взят из границ нормы.");

            var points = new List<ProductivityTrendPointDto>();
            foreach (var date in timeline)
            {
                var active = ActiveNorm(directionNorms, date);
                if (active is null)
                {
                    points.Add(new ProductivityTrendPointDto(date, null, null, null, null, "", "no_norm"));
                    continue;
                }

                usedTypes.Add(active.FormulaType);
                var flag = LogVolumeFlag(active, vStar);
                var hours = HoursAt(active, vStar);
                var perUnit = PerUnit(hours, vStar);
                points.Add(new ProductivityTrendPointDto(
                    date,
                    ToDto(hours),
                    ToDto(perUnit),
                    null,
                    active.Id,
                    active.FormulaType.ToString(),
                    flag));

                if (hours is decimal h)
                    overallHours[date] = overallHours.GetValueOrDefault(date) + h;
            }

            var firstHours = points.Select(p => p.HoursAtVStar).FirstOrDefault(v => v is > 0);
            points = points.Select(p => p with
            {
                Index = firstHours is > 0 && p.HoursAtVStar is double h
                    ? Math.Round(h / firstHours.Value, 4)
                    : null,
            }).ToList();

            byGroup.Add(new ProductivityTrendGroupDto(
                directionId,
                groupName,
                ToDto(vStar),
                vStarSource,
                BuildSegments(points, directionNorms)));

            AppendTransitions(
                directionId,
                groupName,
                directionNorms,
                samples,
                projectVolumes,
                periodStart,
                periodEnd,
                transitions,
                counterfactuals);
        }

        if (byGroup.Count == 0)
        {
            var empty = EmptyResponse(volumeMode, warnings, "Нет групп IIC с нормами в выбранном периоде.");
            await CacheAsync(cacheKey, empty, cancellationToken);
            return empty;
        }

        var overallPoints = BuildOverallPoints(timeline, overallHours);
        var overall = new ProductivityTrendGroupDto(
            0,
            "Все группы",
            null,
            volumeMode,
            [
                new ProductivityTrendSegmentDto(
                    null,
                    "Индекс часов N(V*)",
                    "Index",
                    "#2C3E50",
                    "overall-index",
                    timeline[0],
                    timeline[^1],
                    overallPoints),
            ]);

        var legend = usedTypes
            .OrderBy(t => (int)t)
            .Select(t => new ProductivityTrendLegendDto(
                t.ToString(),
                FormulaTemplates.Get(t).DisplayName,
                ColorFor(t)))
            .ToList();

        var orderedTransitions = transitions
            .OrderByDescending(t => Math.Abs(t.StandardEffectHours ?? 0) * Math.Max(t.WeightHours, 1))
            .ToList();

        var response = new ProductivityTrendResponse(
            volumeMode,
            overall,
            byGroup,
            legend,
            orderedTransitions,
            counterfactuals.Take(40).ToList(),
            warnings.Distinct().ToList());

        await CacheAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private Task CacheAsync(string key, ProductivityTrendResponse response, CancellationToken ct) =>
        _cache.SetPortfolioAsync(key, response, ct);

    private decimal? HoursAt(IicWorkDirectionNorm norm, decimal vStar)
    {
        if (vStar <= 0)
            return null;

        var snap = NormSnapshotFactory.FromEntity(norm);
        var hours = _engine.ComputeForecastHours(
            snap.NormFormula,
            snap.DeviationFormula,
            vStar,
            vStar,
            PlanningZParameters.Default);
        return hours.Base;
    }

    private static decimal? PerUnit(decimal? hours, decimal vStar) =>
        hours is decimal h && vStar > 0 ? h / vStar : null;

    private void AppendTransitions(
        int directionId,
        string groupName,
        IReadOnlyList<IicWorkDirectionNorm> directionNorms,
        IReadOnlyList<VolumeSample> samples,
        IReadOnlyList<decimal> allProjectVolumes,
        DateTime? periodStart,
        DateTime? periodEnd,
        List<ProductivityTrendTransitionDto> transitions,
        List<ProductivityTrendCounterfactualDto> counterfactuals)
    {
        var ordered = directionNorms.OrderBy(n => n.EffectiveFrom).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var oldNorm = ordered[i - 1];
            var newNorm = ordered[i];
            var switchAt = newNorm.EffectiveFrom;
            if (periodStart.HasValue && switchAt < periodStart.Value)
                continue;
            if (periodEnd.HasValue && switchAt > periodEnd.Value)
                continue;

            IReadOnlyList<decimal> oldVolumes = ProjectTotals(samples.Where(s => s.CreatedAt < switchAt));
            IReadOnlyList<decimal> newVolumes = ProjectTotals(samples.Where(s => s.CreatedAt >= switchAt));
            if (oldVolumes.Count == 0)
                oldVolumes = allProjectVolumes;
            if (newVolumes.Count == 0)
                newVolumes = allProjectVolumes;

            var vOld = PickVolume(oldVolumes, "median", oldNorm, out _);
            var vNew = PickVolume(newVolumes, "median", newNorm, out _);
            var vP10 = PickVolume(allProjectVolumes, "p10", oldNorm, out _);
            var vMed = PickVolume(allProjectVolumes, "median", oldNorm, out _);
            var vP90 = PickVolume(allProjectVolumes, "p90", oldNorm, out _);

            var hoursOldP10 = HoursAt(oldNorm, vP10);
            var hoursNewP10 = HoursAt(newNorm, vP10);
            var hoursOldMed = HoursAt(oldNorm, vMed);
            var hoursNewMed = HoursAt(newNorm, vMed);
            var hoursOldP90 = HoursAt(oldNorm, vP90);
            var hoursNewP90 = HoursAt(newNorm, vP90);
            var hoursNewAtOldV = HoursAt(newNorm, vOld);
            var hoursOldAtOldV = HoursAt(oldNorm, vOld);
            var hoursNewAtNewV = HoursAt(newNorm, vNew);

            var standard = hoursNewAtOldV is decimal hn && hoursOldAtOldV is decimal ho
                ? hn - ho
                : (decimal?)null;
            var mix = hoursNewAtNewV is decimal hn2 && hoursNewAtOldV is decimal hn1
                ? hn2 - hn1
                : (decimal?)null;

            var d10 = Delta(hoursNewP10, hoursOldP10);
            var d90 = Delta(hoursNewP90, hoursOldP90);
            var curvesCross = d10 is decimal a && d90 is decimal b && a != 0 && b != 0 && Math.Sign(a) != Math.Sign(b);
            var logLtOne = LogVolumeFlag(oldNorm, vMed) == "log_volume_lt_one"
                           || LogVolumeFlag(newNorm, vMed) == "log_volume_lt_one"
                           || LogVolumeFlag(newNorm, vP10) == "log_volume_lt_one";

            var verdict = ResolveVerdict(standard, curvesCross, hoursOldMed, hoursNewMed);

            transitions.Add(new ProductivityTrendTransitionDto(
                directionId,
                groupName,
                oldNorm.Id,
                newNorm.Id,
                switchAt,
                oldNorm.FormulaType.ToString(),
                newNorm.FormulaType.ToString(),
                ToDto(vOld),
                ToDto(vNew),
                ToDto(hoursOldP10),
                ToDto(hoursNewP10),
                ToDto(hoursOldMed),
                ToDto(hoursNewMed),
                ToDto(hoursOldP90),
                ToDto(hoursNewP90),
                ToDto(standard),
                ToDto(mix),
                (double)(hoursOldAtOldV ?? 0m),
                verdict,
                curvesCross,
                logLtOne));

            foreach (var sample in ProjectVolumeByProject(samples).Take(8))
            {
                if (sample.Volume <= 0)
                    continue;
                var hOld = HoursAt(oldNorm, sample.Volume);
                var hNew = HoursAt(newNorm, sample.Volume);
                if (hOld is null && hNew is null)
                    continue;
                counterfactuals.Add(new ProductivityTrendCounterfactualDto(
                    directionId,
                    groupName,
                    sample.ProjectId,
                    sample.ProjectCode,
                    (double)Math.Round(sample.Volume, 4),
                    ToDto(hOld),
                    ToDto(hNew),
                    ToDto(Delta(hNew, hOld))));
            }
        }
    }

    private static string ResolveVerdict(
        decimal? standard,
        bool curvesCross,
        decimal? hoursOld,
        decimal? hoursNew)
    {
        if (hoursOld is null || hoursNew is null)
            return "insufficient";
        if (curvesCross)
            return "ambiguous";
        if (standard is decimal s)
        {
            if (s < 0) return "tightening";
            if (s > 0) return "loosening";
        }

        return "stable";
    }

    private static IReadOnlyList<ProductivityTrendSegmentDto> BuildSegments(
        IReadOnlyList<ProductivityTrendPointDto> points,
        IReadOnlyList<IicWorkDirectionNorm> norms)
    {
        var byId = norms.ToDictionary(n => n.Id);
        var segments = new List<ProductivityTrendSegmentDto>();
        List<ProductivityTrendPointDto>? bucket = null;
        int? currentNormId = null;
        string currentType = "";

        void Flush()
        {
            if (bucket is null || bucket.Count == 0 || currentNormId is null)
                return;
            byId.TryGetValue(currentNormId.Value, out var norm);
            var type = ParseFormula(currentType);
            segments.Add(new ProductivityTrendSegmentDto(
                currentNormId,
                norm is null ? currentType : $"{FormulaTemplates.Get(norm.FormulaType).DisplayName} #{norm.Id}",
                currentType,
                ColorFor(type),
                $"n{currentNormId}",
                bucket[0].Date,
                bucket[^1].Date,
                bucket.ToList()));
        }

        foreach (var point in points)
        {
            if (point.NormId is null)
            {
                Flush();
                bucket = null;
                currentNormId = null;
                continue;
            }

            if (currentNormId != point.NormId)
            {
                Flush();
                bucket = [];
                currentNormId = point.NormId;
                currentType = point.FormulaType;
            }

            bucket!.Add(point);
        }

        Flush();
        return segments;
    }

    private static List<ProductivityTrendPointDto> BuildOverallPoints(
        IReadOnlyList<DateTime> timeline,
        IReadOnlyDictionary<DateTime, decimal> hoursByDate)
    {
        decimal? baseHours = null;
        foreach (var date in timeline)
        {
            if (hoursByDate.TryGetValue(date, out var h) && h > 0)
            {
                baseHours = h;
                break;
            }
        }

        var points = new List<ProductivityTrendPointDto>(timeline.Count);
        foreach (var date in timeline)
        {
            hoursByDate.TryGetValue(date, out var hours);
            double? hoursDto = hours > 0 ? ToDto(hours) : null;
            double? index = baseHours is > 0 && hours > 0
                ? (double)Math.Round(hours / baseHours.Value, 4)
                : null;
            points.Add(new ProductivityTrendPointDto(date, hoursDto, null, index, null, "Index", "ok"));
        }

        return points;
    }

    private static async Task<List<VolumeSample>> LoadVolumeSamplesAsync(
        AppDbContext db,
        IReadOnlyList<int> versionIds,
        IReadOnlyDictionary<int, VersionRow> versionById,
        CancellationToken ct)
    {
        var samples = new List<VolumeSample>();
        if (versionIds.Count == 0)
            return samples;

        var ppv = await db.ProjectPhysicalVolumes.AsNoTracking()
            .Include(p => p.PhysicalVolumeKfo)
            .Include(p => p.PhysicalVolumeBer)
            .Where(p => versionIds.Contains(p.ProjectAnalysisVersionId))
            .ToListAsync(ct);

        foreach (var row in ppv)
        {
            if (row.GetIsAssessment() != true)
                continue;
            if (row.GetClassifierId() is not int classifierId)
                continue;
            if (!versionById.TryGetValue(row.ProjectAnalysisVersionId, out var version))
                continue;
            var volume = row.Quantity * row.GetCoefficient();
            if (volume <= 0)
                continue;
            samples.Add(new VolumeSample(
                classifierId,
                version.ProjectId,
                version.ProjectCode,
                version.CreatedAt,
                volume));
        }

        var equipment = await db.ProjectEquipments.AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => versionIds.Contains(e.ProjectAnalysisVersionId))
            .ToListAsync(ct);

        foreach (var row in equipment)
        {
            if (row.Equipment is null || row.Equipment.IsAssessment != true)
                continue;
            if (row.Equipment.ClassifierId is not int classifierId)
                continue;
            if (!versionById.TryGetValue(row.ProjectAnalysisVersionId, out var version))
                continue;
            var volume = row.Quantity * row.Equipment.Coefficient;
            if (volume <= 0)
                continue;
            samples.Add(new VolumeSample(
                classifierId,
                version.ProjectId,
                version.ProjectCode,
                version.CreatedAt,
                volume));
        }

        return samples;
    }

    private static List<decimal> ProjectTotals(IEnumerable<VolumeSample> samples) =>
        samples
            .GroupBy(s => s.ProjectId)
            .Select(g => g.Sum(x => x.Volume))
            .Where(v => v > 0)
            .ToList();

    private static IEnumerable<(int ProjectId, string ProjectCode, decimal Volume)> ProjectVolumeByProject(
        IEnumerable<VolumeSample> samples) =>
        samples
            .GroupBy(s => (s.ProjectId, s.ProjectCode))
            .Select(g => (g.Key.ProjectId, g.Key.ProjectCode, g.Sum(x => x.Volume)))
            .Where(x => x.Item3 > 0);

    private static decimal ResolveVStar(
        IReadOnlyList<decimal> projectVolumes,
        string volumeMode,
        IReadOnlyList<IicWorkDirectionNorm> norms,
        out string source) =>
        PickVolume(projectVolumes, volumeMode, norms[0], out source);

    private static decimal PickVolume(
        IReadOnlyList<decimal> volumes,
        string mode,
        IicWorkDirectionNorm fallbackNorm,
        out string source)
    {
        if (volumes.Count > 0)
        {
            source = mode;
            var q = mode.ToLowerInvariant() switch
            {
                "p10" => 0.10,
                "p90" => 0.90,
                _ => 0.50,
            };
            return Quantile(volumes, q);
        }

        source = "fallback_bounds";
        var min = fallbackNorm.MinVolume;
        var max = fallbackNorm.MaxVolume > 0 ? fallbackNorm.MaxVolume : 0m;
        if (max > min && max < 100_000m)
            return (min + max) / 2m;
        return Math.Max(min > 0 ? min : 10m, 10m);
    }

    internal static decimal Quantile(IReadOnlyList<decimal> values, double q)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 1)
            return sorted[0];
        var pos = (sorted.Count - 1) * q;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        if (lo == hi)
            return sorted[lo];
        var t = (decimal)(pos - lo);
        return sorted[lo] + t * (sorted[hi] - sorted[lo]);
    }

    private static List<DateTime> BuildTimeline(
        DateTime? periodStart,
        DateTime? periodEnd,
        IReadOnlyList<IicWorkDirectionNorm> norms)
    {
        var dates = new SortedSet<DateTime>();
        var start = periodStart ?? norms.Min(n => n.EffectiveFrom);
        var end = periodEnd ?? DateTime.UtcNow.Date;
        dates.Add(start);
        dates.Add(end);
        foreach (var norm in norms)
        {
            if (norm.EffectiveFrom >= start && norm.EffectiveFrom <= end)
                dates.Add(norm.EffectiveFrom);
        }

        return dates.ToList();
    }

    private static IicWorkDirectionNorm? ActiveNorm(IReadOnlyList<IicWorkDirectionNorm> norms, DateTime date) =>
        norms
            .Where(n => n.EffectiveFrom <= date && (n.EffectiveTo == null || n.EffectiveTo >= date))
            .OrderByDescending(n => n.EffectiveFrom)
            .FirstOrDefault();

    private static HashSet<int> SelfAndDescendants(int id, IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        var children = byId.Values.Where(d => d.ParentId.HasValue).ToLookup(d => d.ParentId!.Value);
        var set = new HashSet<int> { id };
        var stack = new Stack<int>();
        stack.Push(id);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var child in children[current])
            {
                if (set.Add(child.Id))
                    stack.Push(child.Id);
            }
        }

        return set;
    }

    private static string LogVolumeFlag(IicWorkDirectionNorm norm, decimal vStar)
    {
        if ((norm.FormulaType == FormulaType.Logarithmic || norm.FormulaType == FormulaType.ShiftedLog)
            && vStar < 1m)
            return "log_volume_lt_one";
        return "ok";
    }

    private static string ColorFor(FormulaType type) =>
        FormulaColors.TryGetValue(type, out var color) ? color : "#607d8b";

    private static FormulaType ParseFormula(string value) =>
        Enum.TryParse<FormulaType>(value, out var type) ? type : FormulaType.Linear;

    private static string NormalizeVolumeMode(string? mode) =>
        mode?.Trim().ToLowerInvariant() switch
        {
            "p10" => "p10",
            "p90" => "p90",
            _ => "median",
        };

    private static double? ToDto(decimal? value) =>
        value is decimal v ? (double)Math.Round(v, 2, MidpointRounding.AwayFromZero) : null;

    private static decimal? Delta(decimal? next, decimal? prev) =>
        next is decimal n && prev is decimal p ? n - p : null;

    private static ProductivityTrendResponse EmptyResponse(
        string volumeMode,
        List<string> warnings,
        string extra)
    {
        warnings.Add(extra);
        return new ProductivityTrendResponse(
            volumeMode,
            new ProductivityTrendGroupDto(0, "Все группы", null, volumeMode, []),
            [],
            [],
            [],
            [],
            warnings.Distinct().ToList());
    }

    private sealed record VersionRow(int Id, int ProjectId, DateTime CreatedAt, string ProjectCode);

    private sealed record VolumeSample(
        int ClassifierId,
        int ProjectId,
        string ProjectCode,
        DateTime CreatedAt,
        decimal Volume);
}
