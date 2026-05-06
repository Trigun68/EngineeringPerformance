using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

/// <summary>
/// Результат выбора характеристики и её варианта в диалоге.
/// </summary>
public sealed record ProjectCharacteristicSelectionResult(
    int CharacteristicId,
    string CharacteristicName,
    int OptionId,
    string OptionName,
    decimal? ValuePercent);

public sealed partial class ProjectCharacteristicSelectionEditViewModel : ObservableObject
{
    private readonly IDirectoryCache<ProjectCharacteristic> _cache;
    private IReadOnlyList<ProjectCharacteristic> _allCharacteristics = [];
    private IReadOnlyList<int> _excludedCharacteristicIds = [];
    private Action<ProjectCharacteristicSelectionResult?>? _closeDialog;

    [ObservableProperty]
    private string _windowTitle = "Характеристика проекта";

    [ObservableProperty]
    private ObservableCollection<ProjectCharacteristic> _characteristics = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOptionsAvailable))]
    private ObservableCollection<ProjectCharacteristicOption> _options = [];

    public bool IsOptionsAvailable => Options.Count > 0;

    [ObservableProperty]
    private int? _selectedCharacteristicId;

    [ObservableProperty]
    private int? _selectedOptionId;

    [ObservableProperty]
    private string? _errorMessage;

    public ProjectCharacteristicSelectionEditViewModel(IDirectoryCache<ProjectCharacteristic> cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Вызывается при смене характеристики: сбрасывает варианты и список выбора.
    /// </summary>
    partial void OnSelectedCharacteristicIdChanged(int? value)
    {
        SelectedOptionId = null;
        if (value is null)
        {
            Options = [];
            OnPropertyChanged(nameof(IsOptionsAvailable));
            return;
        }

        var ch = _allCharacteristics.FirstOrDefault(c => c.Id == value.Value);
        Options = ch is null
            ? []
            : new ObservableCollection<ProjectCharacteristicOption>(ch.Options.OrderBy(o => o.SortOrder));
        OnPropertyChanged(nameof(IsOptionsAvailable));
    }

    public async Task InitializeAsync(
        ProjectCharacteristicSelectionRowVm? existing,
        IReadOnlyList<int> excludedCharacteristicIds,
        Action<ProjectCharacteristicSelectionResult?> closeDialog)
    {
        _closeDialog = closeDialog;
        _excludedCharacteristicIds = excludedCharacteristicIds;
        ErrorMessage = null;

        _allCharacteristics = await _cache.GetAllAsync();
        var visible = _allCharacteristics
            .Where(c => existing is not null && c.Id == existing.CharacteristicId
                        || !_excludedCharacteristicIds.Contains(c.Id))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();
        Characteristics = new ObservableCollection<ProjectCharacteristic>(visible);

        if (existing is not null)
        {
            WindowTitle = "Редактирование характеристики проекта";
            SelectedCharacteristicId = existing.CharacteristicId;
            // Options заполняются в OnSelectedCharacteristicIdChanged — устанавливаем выбор после
            SelectedOptionId = existing.OptionId;
            OnPropertyChanged(nameof(IsOptionsAvailable));
        }
        else
        {
            WindowTitle = "Новая характеристика проекта";
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedCharacteristicId is null)
        {
            ErrorMessage = "Выберите характеристику";
            return;
        }
        if (SelectedOptionId is null)
        {
            ErrorMessage = "Выберите вариант";
            return;
        }

        var ch = _allCharacteristics.First(c => c.Id == SelectedCharacteristicId.Value);
        var opt = ch.Options.First(o => o.Id == SelectedOptionId.Value);
        var result = new ProjectCharacteristicSelectionResult(
            ch.Id, ch.Name, opt.Id, opt.Name, opt.ValuePercent);

        _closeDialog?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel() => _closeDialog?.Invoke(null);
}
