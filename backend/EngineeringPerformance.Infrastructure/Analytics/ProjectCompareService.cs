using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сравнение двух версий: итого (IIC), по объектам, детально.
/// Один полный расчёт на пару версий кладётся в кэш поколения проекта (ISS-018).
/// </summary>
public sealed class ProjectCompareService : IProjectCompareService
{
    private readonly ProjectsPageRawDataLoader _rawLoader;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly IProjectAnalyticsCache _cache;
    private readonly AppDbContext _db;

    public ProjectCompareService(
        ProjectsPageRawDataLoader rawLoader,
        IPerformanceCalculationEngine calculationEngine,
        IProjectAnalyticsCache cache,
        AppDbContext db)
    {
        _rawLoader = rawLoader;
        _calculationEngine = calculationEngine;
        _cache = cache;
        _db = db;
    }

    public async Task<VersionCompareResultDto> CompareAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CompareCacheKey(versionAId, versionBId);
        var cached = await _cache.GetAsync<VersionCompareResultDto>(projectId, cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var bundleA = await _rawLoader.LoadAsync(projectId, versionAId, cancellationToken);
        var bundleB = await _rawLoader.LoadAsync(projectId, versionBId, cancellationToken);
        var perfA = ProjectsPageGridBuilder.BuildPerformance(bundleA, _calculationEngine);
        var perfB = ProjectsPageGridBuilder.BuildPerformance(bundleB, _calculationEngine);
        var (facilityA, detailA) = ProjectsPageGridBuilder.BuildFactGrids(bundleA, perfA, _calculationEngine);
        var (facilityB, detailB) = ProjectsPageGridBuilder.BuildFactGrids(bundleB, perfB, _calculationEngine);

        var grids = ProjectsPageGridBuilder.BuildCompareGrids(
            perfA,
            perfB,
            new ProjectsPageFactGridsDto(perfA, facilityA, detailA),
            new ProjectsPageFactGridsDto(perfB, facilityB, detailB),
            bundleA,
            bundleB,
            _calculationEngine);

        var result = new VersionCompareResultDto(grids.Summary, grids.Facility, grids.Detail);
        await _cache.SetAsync(projectId, cacheKey, result, CancellationToken.None);
        return result;
    }

    public async Task<CompareChartResultDto> GetChartAsync(
        int projectId,
        int versionAId,
        int versionBId,
        string level = "iic",
        string metric = "zScore",
        int top = 20,
        CancellationToken cancellationToken = default)
    {
        var compare = await CompareAsync(projectId, versionAId, versionBId, cancellationToken);
        var versionNames = await _db.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId && (v.Id == versionAId || v.Id == versionBId))
            .ToDictionaryAsync(v => v.Id, v => v.VersionName, cancellationToken);

        versionNames.TryGetValue(versionAId, out var nameA);
        versionNames.TryGetValue(versionBId, out var nameB);

        return CompareChartBuilder.FromCompare(
            compare,
            nameA ?? "A",
            nameB ?? "B",
            level,
            metric,
            top);
    }

    internal static string CompareCacheKey(int versionAId, int versionBId) =>
        $"compare:{versionAId}:{versionBId}";
}
