using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectDataDialogViewModel : ObservableObject
{
    private readonly IDirectoryService<Project> _projectService;
    private Action<bool>? _closeDialog;
    private int _projectId;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _workNumber;
    [ObservableProperty] private string? _gip;
    [ObservableProperty] private DateTime? _workStart;
    [ObservableProperty] private string? _workStatus;
    [ObservableProperty] private string? _contractNumber;
    [ObservableProperty] private string? _calendarPlanNumber;
    [ObservableProperty] private string? _type;
    [ObservableProperty] private string? _customer;
    [ObservableProperty] private string? _contractor;
    [ObservableProperty] private string? _program;
    [ObservableProperty] private string? _errorMessage;

    public IReadOnlyList<string> WorkStatusItems { get; } =
    [
        "Черновик",
        "в работе",
        "завершен",
        "отменен",
    ];

    public ProjectDataDialogViewModel(IDirectoryService<Project> projectService)
    {
        _projectService = projectService;
    }

    public void Initialize(Project project, Action<bool> closeDialog)
    {
        _projectId = project.Id;
        _closeDialog = closeDialog;
        ErrorMessage = null;

        Code = project.Code;
        Name = project.Name;
        WorkNumber = project.WorkNumber;
        Gip = project.Gip;
        WorkStart = project.WorkStart?.ToDateTime(TimeOnly.MinValue);
        WorkStatus = project.WorkStatus;
        ContractNumber = project.ContractNumber;
        CalendarPlanNumber = project.CalendarPlanNumber;
        Type = project.Type;
        Customer = project.Customer;
        Contractor = project.Contractor;
        Program = project.Program;
    }

    [RelayCommand]
    private async Task SaveAsync()
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

        try
        {
            var item = await _projectService.GetByIdAsync(_projectId);
            if (item is null)
            {
                ErrorMessage = "Проект не найден";
                return;
            }

            item.Code = Code.Trim();
            item.Name = Name.Trim();
            item.WorkNumber = WorkNumber?.Trim();
            item.Gip = Gip?.Trim();
            item.WorkStart = WorkStart.HasValue ? DateOnly.FromDateTime(WorkStart.Value) : null;
            item.WorkStatus = WorkStatus?.Trim();
            item.ContractNumber = ContractNumber?.Trim();
            item.CalendarPlanNumber = CalendarPlanNumber?.Trim();
            item.Type = Type?.Trim();
            item.Customer = Customer?.Trim();
            item.Contractor = Contractor?.Trim();
            item.Program = Program?.Trim();
            item.ModifiedAt = DateTime.UtcNow;

            await _projectService.UpdateAsync(item);
            _closeDialog?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => _closeDialog?.Invoke(false);
}
