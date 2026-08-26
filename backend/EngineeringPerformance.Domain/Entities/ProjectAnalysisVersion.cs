using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Domain.Entities;

public sealed class ProjectAnalysisVersion : DirectoryItem
{
    public int ProjectId { get; set; }

    public string VersionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AnalysisDirection Direction { get; set; }

    public int? PhysicalVolumeTypeId { get; set; }

    public PlanningScenario PlanningScenario { get; set; } = PlanningScenario.ByEachValue;

    /// <summary>
    /// Основная версия проекта для своего направления (Plan/Fact). Ровно одна основная версия
    /// на направление (Plan и Fact независимо друг от друга); Forecast не участвует.
    /// Используется отчётом «план/факт» (<see cref="EngineeringPerformance.Application.Analytics.ProjectReportSummaryDto"/>).
    /// </summary>
    public bool IsMain { get; set; }

    public Project Project { get; set; } = null!;

    public PhysicalVolumeType? PhysicalVolumeType { get; set; }

    public ICollection<ProjectPhysicalVolume> PhysicalVolumes { get; set; } = [];

    public ICollection<ProjectEquipment> Equipments { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public ICollection<DataImportIteration> ImportIterations { get; set; } = [];
}
