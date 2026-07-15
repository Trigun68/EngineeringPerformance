using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class ProjectPhysicalVolume : DataTableRecord
{
    public int ProjectId { get; set; }

    public int? PhysicalVolumeKfoId { get; set; }

    public int? PhysicalVolumeBerId { get; set; }

    /// <summary>Иерархия строк ФО внутри версии (deep copy сохраняет связь через маппинг ParentId).</summary>
    public int? ParentId { get; set; }

    public decimal Quantity { get; set; }

    public string? FacilityCode { get; set; }

    public string? FacilityName { get; set; }

    public string? BindingType { get; set; }

    public string? DocumentSet { get; set; }

    public string? CatalogId { get; set; }

    public string? Tag { get; set; }

    public string? WorkId { get; set; }

    public string? WorkName { get; set; }

    public string? WorkComment { get; set; }

    public string? EstimationBasis { get; set; }

    public string? CatalogCode { get; set; }

    public string? CatalogName { get; set; }

    public decimal? LaborHoursPerUnit { get; set; }

    public decimal? LaborIntensityBase { get; set; }

    public decimal? LaborIntensityPessimistic { get; set; }

    public decimal? LaborIntensityOptimistic { get; set; }

    public decimal? MachineHoursPerUnit { get; set; }

    public decimal? TotalMachineHours { get; set; }

    public string? Status { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public PhysicalVolumeKfo? PhysicalVolumeKfo { get; set; }

    public PhysicalVolumeBer? PhysicalVolumeBer { get; set; }

    public ProjectPhysicalVolume? Parent { get; set; }

    public ICollection<ProjectPhysicalVolume> Children { get; set; } = [];
}
