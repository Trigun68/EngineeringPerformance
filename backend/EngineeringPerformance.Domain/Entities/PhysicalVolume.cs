using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

[Obsolete("Use PhysicalVolumeKfo or PhysicalVolumeBer. Unified PhysicalVolumes table removed.")]
public sealed class PhysicalVolume : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int VolumeTypeId { get; set; }

    public int? ClassifierId { get; set; }

    /// <summary>Выбранная норма ИИС для классификатора (КФО).</summary>
    public int? IicNormId { get; set; }

    public bool? IsAssessment { get; set; }

    public string? DocumentSet { get; set; }

    /// <summary>Переводит количество по проекту в единицы измерения группы физ. объёмов (ИИС).</summary>
    public decimal Coefficient { get; set; } = 1m;

    /// <summary>Единица измерения из исходного справочника (БЭР24/КФО2). FK на UnitsOfMeasurement.</summary>
    public int? UnitId { get; set; }

    public PhysicalVolume? Parent { get; set; }

    public ICollection<PhysicalVolume> Children { get; set; } = [];

    public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public PhysicalVolumeType VolumeType { get; set; } = null!;

    public IicWorkDirection? Classifier { get; set; }

    public UnitOfMeasurement? Unit { get; set; }
}
