using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Сервис отчёта «Анализ версии проекта». Объединяет:
/// 1) полностью раскрытую иерархию направлений IIC (с ед. изм.),
/// 2) показатели производительности из <see cref="IPerformanceService"/>,
/// 3) ненулевые физ. объёмы проекта по каждому направлению (номенклатура + ед. изм. + кол-во),
/// 4) отдельный блок видов работ из трудоёмкости проекта.
/// </summary>
public sealed class ProjectVersionAnalysisService : IProjectVersionAnalysisService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPerformanceService _performanceService;

    public ProjectVersionAnalysisService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPerformanceService performanceService)
    {
        _contextFactory = contextFactory;
        _performanceService = performanceService;
    }

    public async Task<ProjectVersionAnalysisDto> GenerateReportAsync(int projectId, int versionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Include(v => v.Project)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId)
            ?? throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");

        // Запускаем PerformanceData и запросы к БД параллельно
        var performanceTask = _performanceService.GetPerformanceDataAsync(projectId, versionId);

        var pvTask = context.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(pv => pv.PhysicalVolume).ThenInclude(p => p!.Unit)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var laborTask = context.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.WorkDirection)
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .OrderBy(li => li.DocumentationCode)
            .ToListAsync();

        await Task.WhenAll(performanceTask, pvTask, laborTask);

        var performanceData = performanceTask.Result;
        var projectPhysicalVolumes = pvTask.Result;
        var laborIntensities = laborTask.Result;

        // Группировка ненулевых физ. объёмов по направлению (ClassifierId).
        var pvByDirection = projectPhysicalVolumes
            .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true && pv.Quantity > 0m && pv.PhysicalVolume.IsAssessment == true)
            .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Группировка трудоёмкости по направлению (WorkDirectionId).
        var laborByDirection = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Иерархия направлений: используем результат PerformanceService как готовую упорядоченную структуру.
        var directions = performanceData
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r => new ProjectVersionDirectionDto(
                Id: r.WorkDirectionId,
                ParentId: r.ParentId,
                Level: r.Level,
                Code: r.WorkDirectionCode,
                Name: r.WorkDirectionName,
                UnitName: r.NormUnitName,
                FactPhysicalVolume: r.FactPhysicalVolume,
                FactLaborHours: r.FactLaborHours,
                FactProductivity: r.FactProductivity,
                FactEstimate: r.FactEstimate,
                NormProductivity: r.NormProductivity,
                NormEstimate: r.NormEstimate,
                NormStdDev: r.NormStdDev,
                ZScore: r.ZScore,
                Interpretation: r.Interpretation,
                PhysicalVolumes: BuildPhysicalVolumes(pvByDirection, r.WorkDirectionId),
                DirectionLaborIntensities: BuildDirectionLabor(laborByDirection, r.WorkDirectionId)))
            .ToList();

        // Виды работ: общий блок из трудоёмкости проекта.
        var laborRows = laborIntensities
            .Select(li => new ProjectVersionLaborIntensityDto(
                DocumentationCode: li.DocumentationCode,
                Brand: li.Brand,
                WorkDirectionName: li.WorkDirection?.Name,
                PlannedHours: li.PlannedHours,
                ActualHours: li.ActualHours))
            .ToList();

        return new ProjectVersionAnalysisDto(
            ProjectName: version.Project.Name,
            ProjectCode: version.Project.Code,
            VersionName: version.VersionName,
            Directions: directions,
            LaborIntensities: laborRows);
    }

    private static IReadOnlyList<ProjectVersionPhysicalVolumeDto> BuildPhysicalVolumes(
        IReadOnlyDictionary<int, List<ProjectPhysicalVolume>> pvByDirection,
        int directionId)
    {
        if (!pvByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionPhysicalVolumeDto>();

        return list
            .Select(pv =>
            {
                var coeff = pv.PhysicalVolume?.Coefficient ?? 1m;
                return new ProjectVersionPhysicalVolumeDto(
                    Code: pv.PhysicalVolume?.Code ?? string.Empty,
                    Name: pv.PhysicalVolume?.Name ?? string.Empty,
                    UnitName: pv.PhysicalVolume?.Unit?.Name,
                    Quantity: pv.Quantity,
                    Coefficient: coeff,
                    Total: pv.Quantity * coeff);
            })
            .OrderBy(pv => pv.Code, StringComparer.Ordinal)
            .ThenBy(pv => pv.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<ProjectVersionDirectionLaborDto> BuildDirectionLabor(
        IReadOnlyDictionary<int, List<ProjectLaborIntensity>> laborByDirection,
        int directionId)
    {
        if (!laborByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionDirectionLaborDto>();

        return list
            .Select(li => new ProjectVersionDirectionLaborDto(
                DocumentationCode: li.DocumentationCode,
                PlannedHours: li.PlannedHours))
            .ToList();
    }
}
