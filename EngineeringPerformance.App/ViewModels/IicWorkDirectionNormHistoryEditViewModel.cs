using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IicWorkDirectionNormHistoryEditViewModel : ObservableObject
{
    private readonly IRegisterService<IicWorkDirectionNorm> _service;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private string _workDirectionName = string.Empty;
    [ObservableProperty] private string _unitName = string.Empty;
    [ObservableProperty] private int _workDirectionId;
    [ObservableProperty] private int? _unitId;
    [ObservableProperty] private decimal? _performance;
    [ObservableProperty] private decimal? _estimate;
    [ObservableProperty] private decimal? _standardDeviation;
    [ObservableProperty] private DateTime _effectiveFrom = DateTime.UtcNow;
    [ObservableProperty] private DateTime? _effectiveTo;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _windowTitle = "Новая запись нормы";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _entityId;

    public IicWorkDirectionNormHistoryEditViewModel(IRegisterService<IicWorkDirectionNorm> service)
    {
        _service = service;
    }

    public void PrepareSync(IicWorkDirectionNorm? item, string workDirectionName, string unitName, int workDirectionId, int? unitId, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        WorkDirectionName = workDirectionName;
        UnitName = unitName ?? "—";
        WorkDirectionId = workDirectionId;
        UnitId = unitId;

        if (item is not null)
        {
            EntityId = item.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование записи нормы";
            Performance = item.Performance;
            Estimate = item.Estimate;
            StandardDeviation = item.StandardDeviation;
            EffectiveFrom = item.EffectiveFrom;
            EffectiveTo = item.EffectiveTo;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая запись нормы";
            EntityId = 0;
            Performance = null;
            Estimate = null;
            StandardDeviation = null;
            EffectiveFrom = DateTime.UtcNow;
            EffectiveTo = null;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
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

                existing.Performance = Performance;
                existing.Estimate = Estimate;
                existing.StandardDeviation = StandardDeviation;
                existing.EffectiveFrom = EffectiveFrom;
                existing.EffectiveTo = EffectiveTo;
                existing.ModifiedAt = DateTime.UtcNow;
                await _service.UpdateAsync(existing);
            }
            else
            {
                var newItem = new IicWorkDirectionNorm
                {
                    WorkDirectionId = WorkDirectionId,
                    UnitId = UnitId,
                    Performance = Performance,
                    Estimate = Estimate,
                    StandardDeviation = StandardDeviation,
                    EffectiveFrom = EffectiveFrom,
                    EffectiveTo = EffectiveTo,
                };
                await _service.CreateAsync(newItem);
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
