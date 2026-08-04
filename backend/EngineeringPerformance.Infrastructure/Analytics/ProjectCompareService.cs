using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сравнение двух версий: итого (IIC), по объектам, детально.
/// </summary>
public sealed class ProjectCompareService : IProjectCompareService
{
    private readonly ProjectsPageRawDataLoader _rawLoader;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly AppDbContext _db;

    public ProjectCompareService(
        ProjectsPageRawDataLoader rawLoader,
        IPerformanceCalculationEngine calculationEngine,
        AppDbContext db)
    {
        _rawLoader = rawLoader;
        _calculationEngine = calculationEngine;
        _db = db;
    }

    public async Task<VersionCompareResultDto> CompareAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default)
    {
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

        return new VersionCompareResultDto(grids.Summary, grids.Facility, grids.Detail);
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

        if (string.Equals(level, "facility", StringComparison.OrdinalIgnoreCase))
        {
            var points = compare.Facility
                .Select(f => new CompareChartPointDto(
                    f.FacilityName,
                    null,
                    metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? f.FactLaborHoursA : f.ZScoreA,
                    metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? f.FactLaborHoursB : f.ZScoreB,
                    metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? f.HoursDelta : f.ZDelta))
                .OrderByDescending(p => Math.Abs(p.Delta ?? 0m))
                .Take(Math.Clamp(top, 1, 100))
                .ToList();

            return new CompareChartResultDto("facility", metric, nameA ?? "A", nameB ?? "B", points);
        }

        var iicPoints = compare.Summary
            .Where(r => r.WorkDirectionId >= 0 && !r.IsTotal)
            .Select(r => new CompareChartPointDto(
                r.Name,
                r.WorkDirectionId,
                metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? r.PlannedLaborHoursA : r.FactZScoreA,
                metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? r.PlannedLaborHoursB : r.FactZScoreB,
                metric.Equals("hours", StringComparison.OrdinalIgnoreCase)
                    ? (r.PlannedLaborHoursA.HasValue && r.PlannedLaborHoursB.HasValue
                        ? r.PlannedLaborHoursB - r.PlannedLaborHoursA
                        : null)
                    : r.FactZDelta))
            .OrderByDescending(p => Math.Abs(p.Delta ?? 0m))
            .Take(Math.Clamp(top, 1, 100))
            .ToList();

        return new CompareChartResultDto("iic", metric, nameA ?? "A", nameB ?? "B", iicPoints);
    }
}
