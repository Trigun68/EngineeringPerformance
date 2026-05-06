using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectCharacteristicSelectionRowVm : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _characteristicId;

    [ObservableProperty]
    private string _characteristicName = string.Empty;

    [ObservableProperty]
    private int _optionId;

    [ObservableProperty]
    private string _optionName = string.Empty;

    [ObservableProperty]
    private decimal? _valuePercent;
}

/// <summary>
/// Диалог редактирования набора выбранных характеристик проекта.
/// CRUD строк ведётся в памяти; на «Сохранить» применяется к БД дельтой
/// (delete-orphan + add-new + update-modified) внутри одного контекста.
/// </summary>
public sealed partial class ProjectCharacteristicsDialogViewModel : ObservableObject
{
    private readonly ProjectCharacteristicSelectionService _service;
    private readonly IProjectCharacteristicSelectionEditDialogService _editDialog;
    private readonly List<int> _initialIds = [];
    private Action<bool>? _closeDialog;

    public int ProjectId { get; private set; }

    [ObservableProperty]
    private ObservableCollection<ProjectCharacteristicSelectionRowVm> _rows = [];

    [ObservableProperty]
    private ProjectCharacteristicSelectionRowVm? _selectedRow;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _windowTitle = "Характеристики проекта";

    public ProjectCharacteristicsDialogViewModel(
        ProjectCharacteristicSelectionService service,
        IProjectCharacteristicSelectionEditDialogService editDialog)
    {
        _service = service;
        _editDialog = editDialog;
    }

    public async Task InitializeAsync(int projectId, Action<bool>? closeDialog)
    {
        ProjectId = projectId;
        _closeDialog = closeDialog;
        ErrorMessage = null;
        _initialIds.Clear();

        var existing = await _service.GetByProjectAsync(projectId);
        Rows = new ObservableCollection<ProjectCharacteristicSelectionRowVm>(
            existing.Select(r => new ProjectCharacteristicSelectionRowVm
            {
                Id = r.Id,
                CharacteristicId = r.CharacteristicId,
                CharacteristicName = r.CharacteristicName,
                OptionId = r.OptionId,
                OptionName = r.OptionName,
                ValuePercent = r.ValuePercent,
            }));
        _initialIds.AddRange(existing.Select(r => r.Id));
    }

    private IReadOnlyList<int> SelectedCharacteristicIds(int? exceptRowIndex = null)
    {
        var result = new List<int>(Rows.Count);
        for (var i = 0; i < Rows.Count; i++)
        {
            if (exceptRowIndex.HasValue && i == exceptRowIndex.Value) continue;
            result.Add(Rows[i].CharacteristicId);
        }
        return result;
    }

    [RelayCommand]
    private async Task AddRowAsync()
    {
        var existingChars = SelectedCharacteristicIds();
        var picked = await _editDialog.ShowAsync(null, existingChars);
        if (picked is null) return;

        Rows.Add(new ProjectCharacteristicSelectionRowVm
        {
            Id = 0,
            CharacteristicId = picked.CharacteristicId,
            CharacteristicName = picked.CharacteristicName,
            OptionId = picked.OptionId,
            OptionName = picked.OptionName,
            ValuePercent = picked.ValuePercent,
        });
    }

    [RelayCommand]
    private async Task EditRowAsync()
    {
        if (SelectedRow is null) return;
        var idx = Rows.IndexOf(SelectedRow);
        var existingChars = SelectedCharacteristicIds(idx);
        var picked = await _editDialog.ShowAsync(SelectedRow, existingChars);
        if (picked is null) return;

        SelectedRow.CharacteristicId = picked.CharacteristicId;
        SelectedRow.CharacteristicName = picked.CharacteristicName;
        SelectedRow.OptionId = picked.OptionId;
        SelectedRow.OptionName = picked.OptionName;
        SelectedRow.ValuePercent = picked.ValuePercent;
    }

    [RelayCommand]
    private void DeleteRow()
    {
        if (SelectedRow is null) return;
        Rows.Remove(SelectedRow);
        SelectedRow = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var keepIds = Rows.Where(r => r.Id != 0).Select(r => r.Id).ToHashSet();
            foreach (var id in _initialIds)
            {
                if (!keepIds.Contains(id))
                    await _service.DeleteAsync(id);
            }

            foreach (var row in Rows)
            {
                if (row.Id == 0)
                {
                    var entity = await _service.CreateAsync(ProjectId, row.CharacteristicId, row.OptionId);
                    row.Id = entity.Id;
                }
                else
                {
                    await _service.UpdateAsync(row.Id, row.CharacteristicId, row.OptionId);
                }
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
                    window.DialogResult = result;
            });
        }
    }
}
