using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectAdditionalFieldsViewModel : ObservableObject
{
    private readonly ProjectEditViewModel _parent;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private string? _contractNumber;
    [ObservableProperty] private string? _calendarPlanNumber;
    [ObservableProperty] private string? _type;
    [ObservableProperty] private string? _customer;
    [ObservableProperty] private string? _contractor;
    [ObservableProperty] private string? _program;
    [ObservableProperty] private string? _errorMessage;

    public ProjectAdditionalFieldsViewModel(ProjectEditViewModel parent)
    {
        _parent = parent;
    }

    public void Initialize(Action<bool> closeDialog)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        ContractNumber = _parent.ContractNumber;
        CalendarPlanNumber = _parent.CalendarPlanNumber;
        Type = _parent.Type;
        Customer = _parent.Customer;
        Contractor = _parent.Contractor;
        Program = _parent.Program;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        _parent.ErrorMessage = null;

        _parent.ApplyAdditionalFields(
            ContractNumber,
            CalendarPlanNumber,
            Type,
            Customer,
            Contractor,
            Program);

        await _parent.PersistProjectAsync();

        if (!string.IsNullOrEmpty(_parent.ErrorMessage))
        {
            ErrorMessage = _parent.ErrorMessage;
            return;
        }

        _closeDialog?.Invoke(true);
    }

    [RelayCommand]
    private void CloseWithoutSave()
    {
        _closeDialog?.Invoke(false);
    }
}
