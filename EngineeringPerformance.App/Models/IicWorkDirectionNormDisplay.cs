using CommunityToolkit.Mvvm.ComponentModel;

namespace EngineeringPerformance.App.Models;

public sealed partial class IicWorkDirectionNormDisplay : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _normId;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int? _parentId;

    [ObservableProperty]
    private decimal? _performance;

    [ObservableProperty]
    private decimal? _estimate;

    [ObservableProperty]
    private decimal? _standardDeviation;

    [ObservableProperty]
    private string? _unitName;

    [ObservableProperty]
    private DateTime _effectiveFrom;

    [ObservableProperty]
    private DateTime? _effectiveTo;

    public void CopyValuesFrom(IicWorkDirectionNormDisplay source)
    {
        NormId = source.NormId;
        Code = source.Code;
        Name = source.Name;
        ParentId = source.ParentId;
        Performance = source.Performance;
        Estimate = source.Estimate;
        StandardDeviation = source.StandardDeviation;
        UnitName = source.UnitName;
        EffectiveFrom = source.EffectiveFrom;
        EffectiveTo = source.EffectiveTo;
    }
}
