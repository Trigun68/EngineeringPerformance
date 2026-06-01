using System.ComponentModel;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>Справочник оборудования: иерархия, привязка к группе IIC, коэффициент и признак оценки.</summary>
public sealed class Equipment : DirectoryItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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
}
