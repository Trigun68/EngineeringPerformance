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

    /// <summary>Оценка: да / нет / не выбрано (null).</summary>
    public bool? IsAssessment { get; set; }

    /// <summary>Перевод количества из ед. номенклатуры в ед. группы IIC. Null — не задано.</summary>
    public decimal? Coefficient { get; set; }

    public Equipment? Parent { get; set; }

    public ICollection<Equipment> Children { get; set; } = [];

    public ICollection<ProjectEquipment> ProjectEquipments { get; set; } = [];

    public IicWorkDirection? Classifier { get; set; }

    /// <summary>Классификатор для грида: код и наименование группы IIC с ед. изм.</summary>
    [NotMapped]
    public string ClassifierNameWithUnit { get; set; } = string.Empty;
}
