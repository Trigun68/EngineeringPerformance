using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class TrendAnalyticsService : ITrendAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IFactAssessmentPipeline _pipeline;
    private readonly ProjectsPageRawDataLoader _rawLoader;
    private readonly IProjectAnalyticsCache _cache;
    private readonly IPerformanceCalculationEngine _calculationEngine;

    public TrendAnalyticsService(
        AppDbContext db,
        IFactAssessmentPipeline pipeline,
        ProjectsPageRawDataLoader rawLoader,
        IProjectAnalyticsCache cache,
        IPerformanceCalculationEngine calculationEngine)
    {
        _db = db;
        _pipeline = pipeline;
        _rawLoader = rawLoader;
        _cache = cache;
        _calculationEngine = calculationEngine;
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
        // Ключ версионируется поколением проекта: мутации данных сбрасывают кэш сразу, без TTL.
        var cacheKey = $"trends:{dateFrom:O}:{dateTo:O}:{granularity}:{formulaId}";
        var cached = await _cache.GetAsync<TrendAnalyticsResultDto>(projectId, cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

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

        // Батч-оценка всех версий одним проходом (без N+1 и без повторной оценки в delta-цикле).
        var batch = await _pipeline.EvaluateManyAsync(
            versions.Select(v => new AssessmentRequest(projectId, v.Id, formulaId)).ToList(),
            cancellationToken);

        var factPoints = new List<TrendPointDto>();
        var forecastPoints = new List<TrendPointDto>();
        var deltaPoints = new List<TrendPointDto>();
        var cumulativePoints = new List<TrendPointDto>();
        decimal cumulative = 0m;

        foreach (var version in factVersions)
        {
            if (!batch.Results.TryGetValue(version.Id, out var result))
                continue;
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
            if (!batch.Results.TryGetValue(version.Id, out var result))
                continue;
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
            if (!batch.Results.TryGetValue(fact.Id, out var factResult)
                || !batch.Results.TryGetValue(nearestForecast.Id, out var forecastResult))
                continue;

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

        await _cache.SetAsync(projectId, cacheKey, resultDto, cancellationToken);

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
        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(
            bundle, result.Performance, _calculationEngine);

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
