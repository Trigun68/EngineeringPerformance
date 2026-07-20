using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Номенклатура физических объёмов БЭР (без VolumeTypeId — отдельная таблица).</summary>
public sealed class PhysicalVolumeBer : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? ClassifierId { get; set; }

    /// <summary>Связанная позиция номенклатуры КФО (лист «Спр_Номенклатура ФО», столбец «Код КФО»).</summary>
    public int? PhysicalVolumeKfoId { get; set; }

    public bool? IsAssessment { get; set; }

    public string? DocumentSet { get; set; }

    /// <summary>Переводит количество по проекту в единицы измерения группы физ. объёмов (ИИС).</summary>
    public decimal Coefficient { get; set; } = 1m;

    /// <summary>Единица измерения из исходного справочника БЭР. FK на UnitsOfMeasurement.</summary>
    public int? UnitId { get; set; }

    public PhysicalVolumeBer? Parent { get; set; }

    public ICollection<PhysicalVolumeBer> Children { get; set; } = [];

    public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public IicWorkDirection? Classifier { get; set; }

    public PhysicalVolumeKfo? PhysicalVolumeKfo { get; set; }

    public UnitOfMeasurement? Unit { get; set; }
}
