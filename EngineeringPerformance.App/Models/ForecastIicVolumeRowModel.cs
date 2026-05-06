using CommunityToolkit.Mvvm.ComponentModel;
using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.App.Models;

/// <summary>Строка дерева групп физ. объёмов (ИИС) на форме прогноза; плановый объём задаётся на любом уровне иерархии.</summary>
public sealed partial class ForecastIicVolumeRowModel : ObservableObject
{
    /// <summary>Вызывается при изменении планового объёма (для кнопки «Сохранить»).</summary>
    public Action? PlannedVolumeEditedCallback { get; set; }

    public ForecastIicVolumeRowModel(ForecastIicVolumeRowDto source, bool isLeaf)
    {
        WorkDirectionId = source.WorkDirectionId;
        ParentId = source.ParentId;
        Name = source.Name;
        UnitName = source.UnitName;
        NormProductivity = source.NormProductivity;
        NormEstimate = source.NormEstimate;
        NormStandardDeviation = source.NormStandardDeviation;
        _plannedVolume = source.PlannedVolume;
        BaselinePlannedVolume = source.PlannedVolume;
        OptimisticHours = source.OptimisticHours;
        BaseHours = source.BaseHours;
        PessimisticHours = source.PessimisticHours;
        Range = source.Range;
        HierarchyPath = source.HierarchyPath;
        IsLeaf = isLeaf;
    }

    /// <summary>Значение после последней загрузки с сервера (для отслеживания несохранённых правок).</summary>
    public decimal BaselinePlannedVolume { get; private set; }

    public bool IsPlannedVolumeDirty => PlannedVolume != BaselinePlannedVolume;

    partial void OnPlannedVolumeChanged(decimal value)
    {
        PlannedVolumeEditedCallback?.Invoke();
        OnPropertyChanged(nameof(IsPlannedVolumeDirty));
    }

    public int WorkDirectionId { get; }

    public int? ParentId { get; }

    public string Name { get; }

    public string? UnitName { get; }

    public decimal? NormProductivity { get; }

    public decimal? NormEstimate { get; }

    public decimal? NormStandardDeviation { get; }

    [ObservableProperty]
    private decimal _plannedVolume;

    public decimal OptimisticHours { get; }

    public decimal BaseHours { get; }

    public decimal PessimisticHours { get; }

    public string Range { get; }

    public string HierarchyPath { get; }

    public bool IsLeaf { get; }
}
