using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Номенклатура физических объёмов КФО (без VolumeTypeId — отдельная таблица).</summary>
public sealed class PhysicalVolumeKfo : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? ClassifierId { get; set; }

    /// <summary>Выбранная норма ИИС для классификатора (КФО).</summary>
    public int? IicNormId { get; set; }

    public bool? IsAssessment { get; set; }

    public string? DocumentSet { get; set; }

    /// <summary>Переводит количество по проекту в единицы измерения группы физ. объёмов (ИИС).</summary>
    public decimal Coefficient { get; set; } = 1m;

    /// <summary>Единица измерения из исходного справочника КФО. FK на UnitsOfMeasurement.</summary>
    public int? UnitId { get; set; }

    public PhysicalVolumeKfo? Parent { get; set; }

    public ICollection<PhysicalVolumeKfo> Children { get; set; } = [];

    public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public IicWorkDirection? Classifier { get; set; }

    public UnitOfMeasurement? Unit { get; set; }
}
