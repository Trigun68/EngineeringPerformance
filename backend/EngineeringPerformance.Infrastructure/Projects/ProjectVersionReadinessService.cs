using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

/// <summary>Проверка полноты версии проекта для внешних интеграций (без математики движка).</summary>
public sealed class ProjectVersionReadinessService(AppDbContext context) : IProjectVersionReadinessService
{
    public async Task<ProjectVersionReadinessDto?> GetAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && v.Id == versionId)
            .Select(v => new { v.Id, v.PhysicalVolumeTypeId })
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
            return null;

        var planningParametersCount = await context.ProjectVersionPlanningParameters.AsNoTracking()
            .CountAsync(p => p.ProjectAnalysisVersionId == versionId, cancellationToken);

        var physicalVolumesCount = await context.ProjectPhysicalVolumes.AsNoTracking()
            .CountAsync(p => p.ProjectId == projectId && p.ProjectAnalysisVersionId == versionId, cancellationToken);

        var equipmentCount = await context.ProjectEquipments.AsNoTracking()
            .CountAsync(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId, cancellationToken);

        var laborIntensitiesCount = await context.ProjectLaborIntensities.AsNoTracking()
            .CountAsync(l => l.ProjectId == projectId && l.ProjectAnalysisVersionId == versionId, cancellationToken);

        var hasNormSnapshots = await context.AnalysisVersionSnapshots.AsNoTracking()
            .AnyAsync(s => s.ProjectAnalysisVersionId == versionId, cancellationToken);

        var hasPhysicalVolumeType = version.PhysicalVolumeTypeId.HasValue;
        var issues = new List<ProjectVersionReadinessIssueDto>();

        if (!hasPhysicalVolumeType)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_physical_volume_type",
                "warning",
                "У версии не задан тип физ. объёма (PhysicalVolumeTypeId)."));
        }

        if (planningParametersCount == 0)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_planning_parameters",
                "warning",
                "Нет параметров планирования (Z по направлениям IIC)."));
        }

        if (physicalVolumesCount == 0)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_physical_volumes",
                "info",
                "Нет проектных физ. объёмов (PPV)."));
        }

        if (equipmentCount == 0)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_equipment",
                "info",
                "Нет проектного оборудования."));
        }

        if (laborIntensitiesCount == 0)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_labor_intensities",
                "info",
                "Нет проектных трудоёмкостей."));
        }

        if (!hasNormSnapshots)
        {
            issues.Add(new ProjectVersionReadinessIssueDto(
                "missing_norm_snapshots",
                "warning",
                "Нет снимка норм IIC (AnalysisVersionSnapshots)."));
        }

        return new ProjectVersionReadinessDto(
            projectId,
            versionId,
            hasPhysicalVolumeType,
            planningParametersCount,
            physicalVolumesCount,
            equipmentCount,
            laborIntensitiesCount,
            hasNormSnapshots,
            issues);
    }
}
