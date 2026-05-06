using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PhysicalVolumeTypeEditViewModel : ObservableObject
{
    private readonly IDirectoryService<PhysicalVolumeType> _service;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _windowTitle = "Новый тип физического объема";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public PhysicalVolumeTypeEditViewModel(IDirectoryService<PhysicalVolumeType> service)
    {
        _service = service;
    }

    public async Task InitializeAsync(PhysicalVolumeType? entity = null, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование вида справочника физ. объёмов";
            EntityId = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            Description = entity.Description;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новый вид справочника физ. объёмов";
            EntityId = 0;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Наименование обязательно";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                var existing = await _service.GetByIdAsync(EntityId);
                if (existing is not null)
                {
                    existing.Code = Code.Trim();
                    existing.Name = Name.Trim();
                    existing.Description = Description.Trim();
                    existing.ModifiedAt = DateTime.UtcNow;
                    await _service.UpdateAsync(existing);
                }
            }
            else
            {
                var entity = new PhysicalVolumeType
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Description = Description.Trim(),
                };

                await _service.CreateAsync(entity);
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
