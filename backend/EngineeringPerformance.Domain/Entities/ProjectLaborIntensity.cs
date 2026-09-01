using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class ProjectLaborIntensity : DataTableRecord
{
    public int ProjectId { get; set; }

    public string DocumentationCode { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на направление работ IIC. NULL допустим: при импорте из файла проекта возможно
    /// отсутствие соответствия для строки (не найден `DocumentTypeName` в каталоге «Трудоёмкость»
    /// или у найденной позиции не задан `IicWorkDirectionId`). История сохраняется без привязки.
    /// </summary>
    public int? WorkDirectionId { get; set; }

    /// <summary>
    /// Нормализованная ссылка на справочник «Трудоёмкость», полученная при импорте по полю
    /// <see cref="LaborIntensity.DocumentTypeName"/>. Не обязательная — для исторических записей
    /// до изменения схемы остаётся <c>NULL</c>.
    /// </summary>
    public int? LaborIntensityId { get; set; }

    /// <summary>FK на номенклатуру КФО. NULL — привязка к БЕР или отсутствует.</summary>
    public int? PhysicalVolumeKfoId { get; set; }

    /// <summary>FK на номенклатуру БЕР. NULL — привязка к КФО или отсутствует.</summary>
    public int? PhysicalVolumeBerId { get; set; }

    /// <summary>FK на строку <see cref="ProjectPhysicalVolume"/> в версии (deep copy ремапит через ppvIdMap).</summary>
    public int? ProjectPhysicalVolumeId { get; set; }

    /// <summary>FK на справочник комплектов документации. NULL — карантин, часы не распределяются.</summary>
    public int? DocSetId { get; set; }

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

    public DocSet? DocSet { get; set; }
}
