using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectEquipmentQuickEditViewModel : ObservableObject
{
    private readonly IDataTableService<ProjectEquipment> _service;
    private int _entityId;

    [ObservableProperty] private string _nomenclature = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private string? _errorMessage;

    public ProjectEquipmentQuickEditViewModel(IDataTableService<ProjectEquipment> service)
    {
        _service = service;
    }

    public void Prepare(int projectEquipmentId, string nomenclature, decimal quantity)
    {
        _entityId = projectEquipmentId;
        Nomenclature = nomenclature;
        Quantity = quantity;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var entity = await _service.GetByIdAsync(_entityId);
            if (entity is null)
            {
                ErrorMessage = "Запись не найдена";
                return;
            }
            entity.Quantity = Quantity;
            await _service.UpdateAsync(entity);
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
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                window.DialogResult = result;
        });
    }
}
