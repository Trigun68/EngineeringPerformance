using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class CharacteristicOptionRow : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComparisonAccuracy))]
    private decimal? _valuePercent;

    public string ComparisonAccuracy => Core.ComparisonAccuracy.FromValuePercent(ValuePercent);
}

public sealed partial class ProjectCharacteristicEditViewModel : ObservableObject
{
    private readonly ProjectCharacteristicService _service;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CharacteristicOptionRow> _options = [];

    [ObservableProperty]
    private CharacteristicOptionRow? _selectedOption;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _windowTitle = "Новая характеристика проекта";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public IReadOnlyList<string> AccuracyOptions { get; } =
    [
        Core.ComparisonAccuracy.VeryHigh,
        Core.ComparisonAccuracy.High,
        Core.ComparisonAccuracy.Standard,
        Core.ComparisonAccuracy.Reduced,
    ];

    public ProjectCharacteristicEditViewModel(ProjectCharacteristicService service)
    {
        _service = service;
    }

    public void Initialize(ProjectCharacteristic? item, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;

        if (item is not null)
        {
            EntityId = item.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование характеристики проекта";
            Name = item.Name;
            Options = new ObservableCollection<CharacteristicOptionRow>(
                item.Options
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new CharacteristicOptionRow
                    {
                        Name = o.Name,
                        ValuePercent = o.ValuePercent,
                    }));
        }
        else
        {
            EntityId = 0;
            IsEditMode = false;
            WindowTitle = "Новая характеристика проекта";
            Name = string.Empty;
            Options = [];
        }
    }

    [RelayCommand]
    private void AddOption()
    {
        var row = new CharacteristicOptionRow { Name = string.Empty };
        Options.Add(row);
        SelectedOption = row;
    }

    [RelayCommand]
    private void DeleteOption()
    {
        if (SelectedOption is null) return;
        Options.Remove(SelectedOption);
        SelectedOption = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Наименование обязательно";
            return;
        }

        var optionEntities = Options
            .Where(o => !string.IsNullOrWhiteSpace(o.Name))
            .Select(o => new ProjectCharacteristicOption
            {
                Name = o.Name.Trim(),
                ValuePercent = o.ValuePercent,
            })
            .ToList();

        try
        {
            if (IsEditMode)
            {
                var existing = await _service.GetByIdAsync(EntityId);
                if (existing is null)
                {
                    ErrorMessage = "Запись не найдена";
                    return;
                }

                existing.Name = Name.Trim();
                existing.Options = [];
                await _service.UpdateWithOptionsAsync(existing, optionEntities);
            }
            else
            {
                var item = new ProjectCharacteristic
                {
                    Name = Name.Trim(),
                };
                await _service.CreateWithOptionsAsync(item, optionEntities);
            }

            CloseDialog(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
        {
            _closeDialog(result);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                {
                    window.DialogResult = result;
                }
            });
        }
    }
}
