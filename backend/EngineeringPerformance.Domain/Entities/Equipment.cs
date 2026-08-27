using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Справочник оборудования: иерархия в рамках проекта, привязка к группе IIC, коэффициент и признак оценки.</summary>
public sealed class Equipment : DirectoryItem
{
    public string Description { get; set; } = string.Empty;

    /// <summary>Проект, которому принадлежит позиция справочника. NULL — legacy/orphans (номенклатура).</summary>
    public int? ProjectId { get; set; }

    /// <summary>Группа физ. объёмов (IIC) для расчёта норм.</summary>
    public int? ClassifierId { get; set; }

    /// <summary>SHA-256 (ProjectId?\0 + Name + \0 + UnitId) — уникальный ключ в рамках проекта.</summary>
    public string CatalogKeyHash { get; set; } = string.Empty;

    /// <summary>Оценка: да / нет / не выбрано (null).</summary>
    public bool? IsAssessment { get; set; }

    /// <summary>Единица измерения номенклатуры (из Excel «Размерность»). FK на UnitsOfMeasurement.</summary>
    public int? UnitId { get; set; }

    /// <summary>Перевод количества из ед. номенклатуры в ед. группы IIC.</summary>
    public decimal Coefficient { get; set; } = 1m;

    public Equipment? Parent { get; set; }

    public ICollection<Equipment> Children { get; set; } = [];

    public ICollection<ProjectEquipment> ProjectEquipments { get; set; } = [];

    public IicWorkDirection? Classifier { get; set; }

    public UnitOfMeasurement? Unit { get; set; }

    public Project? Project { get; set; }
}
