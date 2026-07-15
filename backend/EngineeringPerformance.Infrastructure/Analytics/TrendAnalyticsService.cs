using System.Text.Json;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class TrendAnalyticsService : ITrendAnalyticsService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _db;
    private readonly IFactAssessmentPipeline _pipeline;
    private readonly ProjectsPageRawDataLoader _rawLoader;
    private readonly IDistributedCache _cache;

    public TrendAnalyticsService(
        AppDbContext db,
        IFactAssessmentPipeline pipeline,
        ProjectsPageRawDataLoader rawLoader,
        IDistributedCache cache)
    {
        _db = db;
        _pipeline = pipeline;
        _rawLoader = rawLoader;
        _cache = cache;
    }

    public IReadOnlyList<AssessmentFormulaDescriptor> ListFormulas() => _pipeline.ListFormulas();

    public async Task<TrendAnalyticsResultDto> GetTrendsAsync(
        int projectId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string granularity,
        string formulaId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"trends:{projectId}:{dateFrom:O}:{dateTo:O}:{granularity}:{formulaId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<TrendAnalyticsResultDto>(cached)!;

        var versionsQuery = _db.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived);

        if (dateFrom.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            versionsQuery = versionsQuery.Where(v => v.CreatedAt <= dateTo.Value);

        var versions = await versionsQuery
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        var factVersions = versions.Where(v => v.Direction == AnalysisDirection.Fact).ToList();
        var forecastVersions = versions
            .Where(v => v.Direction is AnalysisDirection.Forecast or AnalysisDirection.Plan)
            .ToList();

        var factPoints = new List<TrendPointDto>();
        var forecastPoints = new List<TrendPointDto>();
        var deltaPoints = new List<TrendPointDto>();
        var cumulativePoints = new List<TrendPointDto>();
        decimal cumulative = 0m;

        foreach (var version in factVersions)
        {
            var result = await _pipeline.EvaluateAsync(
                new AssessmentRequest(projectId, version.Id, formulaId),
                cancellationToken);
            var hours = result.Summary.TotalFactLaborHours;
            factPoints.Add(new TrendPointDto(
                version.CreatedAt,
                version.Id,
                version.VersionName,
                "factLaborHours",
                hours,
                version.VersionName));
            cumulative += hours ?? 0m;
            cumulativePoints.Add(new TrendPointDto(
                version.CreatedAt,
                version.Id,
                version.VersionName,
                "cumulativeFactHours",
                Math.Round(cumulative, 2),
                version.VersionName));
        }

        foreach (var version in forecastVersions)
        {
            var result = await _pipeline.EvaluateAsync(
                new AssessmentRequest(projectId, version.Id, formulaId),
                cancellationToken);
            forecastPoints.Add(new TrendPointDto(
                version.CreatedAt,
                version.Id,
                version.VersionName,
                "forecastLaborHours",
                result.Summary.TotalFactLaborHours,
                version.VersionName));
        }

        foreach (var fact in factVersions)
        {
            var nearestForecast = forecastVersions
                .Where(v => v.CreatedAt <= fact.CreatedAt)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault()
                ?? forecastVersions.FirstOrDefault();
            if (nearestForecast is null) continue;

            var factResult = await _pipeline.EvaluateAsync(
                new AssessmentRequest(projectId, fact.Id, formulaId),
                cancellationToken);
            var forecastResult = await _pipeline.EvaluateAsync(
                new AssessmentRequest(projectId, nearestForecast.Id, formulaId),
                cancellationToken);
            var factH = factResult.Summary.TotalFactLaborHours ?? 0m;
            var forecastH = forecastResult.Summary.TotalFactLaborHours ?? 0m;
            deltaPoints.Add(new TrendPointDto(
                fact.CreatedAt,
                fact.Id,
                fact.VersionName,
                "hoursDelta",
                Math.Round(factH - forecastH, 2),
                fact.VersionName));
        }

        var importEvents = await _db.DataImportIterations
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.ImportedAt)
            .Select(i => new TrendEventDto(
                i.ImportedAt,
                "import",
                $"{i.DataType} ({i.RecordCount} зап.)",
                i.ProjectAnalysisVersionId))
            .ToListAsync(cancellationToken);

        var versionEvents = versions.Select(v => new TrendEventDto(
            v.CreatedAt,
            "versionCreated",
            $"{v.Direction}: {v.VersionName}",
            v.Id)).ToList();

        var formula = ListFormulas().First(f => f.Id == formulaId);
        var resultDto = new TrendAnalyticsResultDto(
            granularity,
            formula,
            [
                new TrendSeriesDto("factLaborHours", "Факт трудоёмкость", factPoints),
                new TrendSeriesDto("forecastLaborHours", "Прогноз/план трудоёмкость", forecastPoints),
                new TrendSeriesDto("hoursDelta", "Δ Факт − Прогноз", deltaPoints),
                new TrendSeriesDto("cumulativeFactHours", "Накопительный факт", cumulativePoints),
            ],
            importEvents.Concat(versionEvents).OrderBy(e => e.Timestamp).ToList());

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(resultDto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return resultDto;
    }

    public async Task<IReadOnlyList<TrendDetailRowDto>> GetDetailAsync(
        int projectId,
        int versionId,
        string formulaId,
        CancellationToken cancellationToken = default)
    {
        var version = await _db.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken)
            ?? throw new InvalidOperationException("Version not found.");

        var result = await _pipeline.EvaluateAsync(
            new AssessmentRequest(projectId, versionId, formulaId),
            cancellationToken);

        var bundle = await _rawLoader.LoadAsync(projectId, versionId, cancellationToken);
        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, result.Performance);

        return detail.Select(d => new TrendDetailRowDto(
            version.CreatedAt,
            versionId,
            d.FacilityCode,
            d.FacilityName,
            d.PvCode,
            d.PvName,
            d.FactLaborHours,
            d.ZScore,
            d.Interpretation)).ToList();
    }
}
