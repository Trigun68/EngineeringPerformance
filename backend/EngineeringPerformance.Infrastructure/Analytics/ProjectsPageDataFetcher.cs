using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Единая точка загрузки 9 гридов: 3 SQL-батча на версию + CPU-сборка через Task.WhenAll.
/// </summary>
public sealed class ProjectsPageDataFetcher : IProjectsPageDataFetcher
{
    private readonly ProjectsPageRawDataLoader _loader;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly IProjectAnalyticsCache _cache;

    public ProjectsPageDataFetcher(
        ProjectsPageRawDataLoader loader,
        IPerformanceCalculationEngine calculationEngine,
        IProjectAnalyticsCache cache)
    {
        _loader = loader;
        _calculationEngine = calculationEngine;
        _cache = cache;
    }

    public async Task<ProjectsPageDataEncodedDto> GetEncodedPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var encoded = await GetFullEncodedPageDataAsync(projectId, versionId, compareVersionId, cancellationToken);
        return encoded.WithoutDetailRows();
    }

    public async Task<ProjectsPageDetailsEncodedDto> GetEncodedPageDetailsAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var encoded = await GetFullEncodedPageDataAsync(projectId, versionId, compareVersionId, cancellationToken);
        return new ProjectsPageDetailsEncodedDto(
            encoded.Forecast.Detail,
            encoded.Fact.Detail,
            encoded.Compare?.Detail);
    }

    /// <summary>
    /// Полный упакованный ответ в кэше (включая detail). HTTP режет строки «Детально»
    /// на границе, чтобы не паковать заново и не держать два ключа.
    /// </summary>
    private async Task<ProjectsPageDataEncodedDto> GetFullEncodedPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"page-data-encoded-v2:{VersionKey(versionId, compareVersionId)}";
        var cached = await _cache.GetAsync<ProjectsPageDataEncodedDto>(projectId, cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var encoded = ProjectsPageDataEncodedDto.From(
            await GetProjectsPageDataAsync(projectId, versionId, compareVersionId, cancellationToken));

        // CancellationToken.None: клиент мог уйти, но 6 МБ encoded всё равно пишем для вкладки «Детально».
        await _cache.SetAsync(projectId, cacheKey, encoded, CancellationToken.None);
        return encoded;
    }

    public async Task<ProjectsPageDataDto> GetProjectsPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        // Неупакованный DTO больше не кладём в Redis: это ~50 МБ JSON на demo и срывало
        // HTTP при отмене. UI берёт page-data-encoded-v2; Excel/PDF считают из SQL.
        var primaryBundle = await _loader.LoadAsync(projectId, versionId, cancellationToken);

        ProjectsPageVersionBundle? compareBundle = null;
        if (compareVersionId is int compareId && compareId != versionId)
        {
            compareBundle = await _loader.LoadAsync(projectId, compareId, cancellationToken);
        }

        // CPU-bound сборка гридов параллельно (без доп. round-trips к БД).
        var primaryPerformance = ProjectsPageGridBuilder.BuildPerformance(primaryBundle, _calculationEngine);

        var forecastTask = Task.Run(() =>
        {
            return ProjectsPageGridBuilder.BuildForecastGrids(primaryBundle, primaryPerformance, _calculationEngine);
        }, cancellationToken);

        var factTask = Task.Run(() =>
        {
            var (facility, detail) = ProjectsPageGridBuilder.BuildFactGrids(
                primaryBundle, primaryPerformance, _calculationEngine);
            var performanceWithPlan = ProjectsPageGridBuilder.AttachLivePlanHours(
                primaryPerformance, primaryBundle, _calculationEngine);
            var performanceWithTotals = ProjectsPageGridBuilder.AppendPerformanceTotals(performanceWithPlan);
            return new ProjectsPageFactGridsDto(performanceWithTotals, facility, detail);
        }, cancellationToken);

        await Task.WhenAll(forecastTask, factTask);

        ProjectsPageCompareGridsDto? compare = null;
        if (compareBundle is not null)
        {
            var comparePerformance = ProjectsPageGridBuilder.BuildPerformance(compareBundle, _calculationEngine);
            var (compareFacility, compareDetail) = ProjectsPageGridBuilder.BuildFactGrids(
                compareBundle,
                comparePerformance,
                _calculationEngine);
            var compareFact = new ProjectsPageFactGridsDto(comparePerformance, compareFacility, compareDetail);

            // Сравнение строится по «сырым» performance (без итогов); итоги добавляет BuildCompareGrids.
            compare = ProjectsPageGridBuilder.BuildCompareGrids(
                primaryPerformance,
                comparePerformance,
                new ProjectsPageFactGridsDto(primaryPerformance, factTask.Result.Facility, factTask.Result.Detail),
                compareFact,
                primaryBundle,
                compareBundle,
                _calculationEngine);
        }

        var data = new ProjectsPageDataDto(forecastTask.Result, factTask.Result, compare);
        return data;
    }

    private static string VersionKey(int versionId, int? compareVersionId) =>
        $"v{versionId}:c{compareVersionId?.ToString() ?? "none"}";
}
