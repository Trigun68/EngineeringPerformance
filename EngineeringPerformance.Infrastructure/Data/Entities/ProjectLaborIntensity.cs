using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class ProjectLaborIntensity : DataTableRecord
{
    public int ProjectId { get; set; }

    public string DocumentationCode { get; set; } = string.Empty;

    public int? WorkDirectionId { get; set; }

    public int? LaborIntensityId { get; set; }

    public int? PhysicalVolumeKfoId { get; set; }

    public int? PhysicalVolumeBerId { get; set; }

    public int? ProjectPhysicalVolumeId { get; set; }

    public decimal PlannedHours { get; set; }

    public decimal ActualHours { get; set; }

    public string? Notes { get; set; }

    public string? Brand { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public IicWorkDirection? WorkDirection { get; set; }

    public LaborIntensity? LaborIntensity { get; set; }

    public PhysicalVolumeKfo? PhysicalVolumeKfo { get; set; }

    public PhysicalVolumeBer? PhysicalVolumeBer { get; set; }

    public ProjectPhysicalVolume? ProjectPhysicalVolume { get; set; }
}
