using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сравнение двух версий: итого (IIC), по объектам, детально.
/// </summary>
public sealed class ProjectCompareService : IProjectCompareService
{
    private readonly IPerformanceService _performanceService;
    private readonly ProjectVersionGridDataLoader _gridLoader;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly AppDbContext _db;

    public ProjectCompareService(
        IPerformanceService performanceService,
        ProjectVersionGridDataLoader gridLoader,
        IPerformanceCalculationEngine calculationEngine,
        AppDbContext db)
    {
        _performanceService = performanceService;
        _gridLoader = gridLoader;
        _calculationEngine = calculationEngine;
        _db = db;
    }

    public async Task<VersionCompareResultDto> CompareAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default)
    {
        var summary = await BuildSummaryAsync(projectId, versionAId, versionBId);

        var factA = await LoadFactAssessmentAsync(projectId, versionAId, cancellationToken);
        var factB = await LoadFactAssessmentAsync(projectId, versionBId, cancellationToken);

        var facility = VersionCompareMerge.BuildFacilityRows(factA.Facility, factB.Facility);
        var detail = VersionCompareMerge.BuildDetailRows(factA.Detail, factB.Detail);

        return new VersionCompareResultDto(summary, facility, detail);
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
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r => new CompareChartPointDto(
                r.Name,
                r.WorkDirectionId,
                metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? null : r.ZScoreA,
                metric.Equals("hours", StringComparison.OrdinalIgnoreCase) ? null : r.ZScoreB,
                r.ZDelta))
            .OrderByDescending(p => Math.Abs(p.Delta ?? 0m))
            .Take(Math.Clamp(top, 1, 100))
            .ToList();

        return new CompareChartResultDto("iic", metric, nameA ?? "A", nameB ?? "B", iicPoints);
    }

    private async Task<IReadOnlyList<VersionCompareRowDto>> BuildSummaryAsync(
        int projectId,
        int versionAId,
        int versionBId)
    {
        var dataA = await _performanceService.GetPerformanceDataAsync(projectId, versionAId);
        var dataB = await _performanceService.GetPerformanceDataAsync(projectId, versionBId);

        var dictA = dataA.GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var dictB = dataB.GroupBy(r => r.WorkDirectionId).ToDictionary(g => g.Key, g => g.First());
        var allIds = dictA.Keys.Union(dictB.Keys).ToList();

        var rows = new List<VersionCompareRowDto>(allIds.Count);
        foreach (var id in allIds)
        {
            dictA.TryGetValue(id, out var a);
            dictB.TryGetValue(id, out var b);

            if (a is null && b is null)
                continue;

            var row = a ?? b!;
            decimal? zDelta = a?.ZScore.HasValue == true && b?.ZScore.HasValue == true
                ? b.ZScore - a.ZScore
                : null;

            rows.Add(new VersionCompareRowDto(
                row.WorkDirectionId,
                row.ParentId,
                row.WorkDirectionName,
                a?.ZScore,
                b?.ZScore,
                zDelta,
                a?.Interpretation.ToRussian() is { Length: > 0 } ia ? ia : "-",
                b?.Interpretation.ToRussian() is { Length: > 0 } ib ? ib : "-"));
        }

        return rows;
    }

    private async Task<(List<FactDetailRowInternal> Detail, List<FactFacilityRowInternal> Facility)> LoadFactAssessmentAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var perfData = await _performanceService.GetPerformanceDataAsync(projectId, versionId);
        var grid = await _gridLoader.LoadAsync(
            projectId,
            versionId,
            new ProjectVersionGridLoadOptions { IncludeLaborIntensities = true },
            cancellationToken);

        static string ExtractDocSet(string? code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            var pipeIdx = code.IndexOf('|');
            return pipeIdx >= 0 ? code[(pipeIdx + 1)..].Trim() : string.Empty;
        }

        var laborIntensities = grid.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .Select(li => new FactLaborIntensityRow(
                ExtractDocSet(li.DocumentationCode),
                li.WorkDirectionId,
                li.PlannedHours))
            .ToList();

        var normByDirection = perfData
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        var input = FactAssessmentEngineMapper.ToInput(
            grid.AllPhysicalVolumes,
            grid.AllEquipment,
            laborIntensities,
            normByDirection,
            grid.ClassifierMap);

        var calculated = _calculationEngine.CalculateFactAssessment(input);
        return FactAssessmentEngineMapper.FromResult(calculated);
    }
}
