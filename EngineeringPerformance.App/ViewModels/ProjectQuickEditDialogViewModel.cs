using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectQuickEditDialogViewModel : ObservableObject
{
    private Action<bool>? _closeDialog;

    [ObservableProperty] private string _windowTitle = "Новый проект";
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _errorMessage;

    public ProjectQuickEditDialogViewModel(Project? existing)
    {
        if (existing is not null)
        {
            WindowTitle = "Редактирование проекта";
            Code = existing.Code;
            Name = existing.Name;
        }
    }

    public void Initialize(Action<bool> closeDialog)
    {
        _closeDialog = closeDialog;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Укажите код проекта";
            return;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Укажите наименование проекта";
            return;
        }
        _closeDialog?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => _closeDialog?.Invoke(false);
}
