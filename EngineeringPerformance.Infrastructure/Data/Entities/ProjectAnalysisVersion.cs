using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class ProjectAnalysisVersion : DirectoryItem
{
    public int ProjectId { get; set; }

    public string VersionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AnalysisDirection Direction { get; set; }

    public string DirectionName => Direction switch
    {
        AnalysisDirection.Plan => "План",
        AnalysisDirection.Fact => "Факт",
        _ => Direction.ToString(),
    };

    public int? PhysicalVolumeTypeId { get; set; }

    public Project Project { get; set; } = null!;

    public PhysicalVolumeType? PhysicalVolumeType { get; set; }

    public ICollection<ProjectPhysicalVolume> PhysicalVolumes { get; set; } = [];

    public ICollection<ProjectEquipment> Equipments { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public ICollection<DataImportIteration> ImportIterations { get; set; } = [];
}
