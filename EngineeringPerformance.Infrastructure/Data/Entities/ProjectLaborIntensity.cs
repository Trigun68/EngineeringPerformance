using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

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

    /// <summary>
    /// V2-D: явный FK на <see cref="PhysicalVolume"/>. NULL — трудоёмкость не привязана к
    /// конкретному физ. объёму (legacy-записи или общий раздел). При удалении
    /// <see cref="PhysicalVolume"/> значение обнуляется на уровне БД (ON DELETE SET NULL),
    /// история трудозатрат сохраняется без привязки.
    /// </summary>
    public int? PhysicalVolumeId { get; set; }

    public decimal PlannedHours { get; set; }

    public decimal ActualHours { get; set; }

    public string? Notes { get; set; }

    public string? Brand { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public IicWorkDirection? WorkDirection { get; set; }

    public LaborIntensity? LaborIntensity { get; set; }

    public PhysicalVolume? PhysicalVolume { get; set; }
}
