using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>Справочник оборудования: иерархия, привязка к группе IIC, коэффициент и признак оценки.</summary>
public sealed class Equipment : DirectoryItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>После точечного обновления строки в гриде — уведомить биндинги.</summary>
    public void NotifyRowRefreshed()
    {
        var handler = PropertyChanged;
        if (handler is null)
            return;

        void Raise(string name) => handler(this, new PropertyChangedEventArgs(name));

        Raise(nameof(Name));
        Raise(nameof(Description));
        Raise(nameof(ClassifierId));
        Raise(nameof(Classifier));
        Raise(nameof(ClassifierNameWithUnit));
        Raise(nameof(UnitId));
        Raise(nameof(Unit));
        Raise(nameof(UnitName));
        Raise(nameof(IsAssessment));
        Raise(nameof(Coefficient));
        Raise(nameof(ParentId));
        Raise(nameof(SortOrder));
        Raise(nameof(IsArchived));
        Raise(nameof(CreatedAt));
        Raise(nameof(ModifiedAt));
    }

    public string Description { get; set; } = string.Empty;

    /// <summary>Группа физ. объёмов (IIC) для расчёта норм.</summary>
    public int? ClassifierId { get; set; }

    /// <summary>SHA-256 (Name + UnitId) — уникальный ключ без индекса по длинному Name.</summary>
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

    /// <summary>Классификатор для грида: код и наименование группы IIC с ед. изм.</summary>
    [NotMapped]
    public string ClassifierNameWithUnit { get; set; } = string.Empty;

    public UnitOfMeasurement? Unit { get; set; }

    /// <summary>Ед. изм. для грида и экспорта.</summary>
    [NotMapped]
    public string UnitName { get; set; } = string.Empty;
}
