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

    public ProjectsPageDataFetcher(
        ProjectsPageRawDataLoader loader,
        IPerformanceCalculationEngine calculationEngine)
    {
        _loader = loader;
        _calculationEngine = calculationEngine;
    }

    public async Task<ProjectsPageDataDto> GetProjectsPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
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
            var performanceWithTotals = ProjectsPageGridBuilder.AppendPerformanceTotals(primaryPerformance);
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

        return new ProjectsPageDataDto(forecastTask.Result, factTask.Result, compare);
    }
}
