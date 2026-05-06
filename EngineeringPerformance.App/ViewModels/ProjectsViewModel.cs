using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectsViewModel : DirectoryViewModelBase<Project>
{
    public Func<Project?, Task>? OpenEditWindow { get; set; }

    public ProjectsViewModel(IDirectoryService<Project> service) : base(service)
    {
    }

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
            await OpenEditWindow(null);
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null || OpenEditWindow is null) return;

        await OpenEditWindow(SelectedItem);
    }

    protected override void ApplyRefreshedItem(Project existing, Project updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Code = updated.Code;
        existing.WorkNumber = updated.WorkNumber;
        existing.ContractNumber = updated.ContractNumber;
        existing.CalendarPlanNumber = updated.CalendarPlanNumber;
        existing.Type = updated.Type;
        existing.Customer = updated.Customer;
        existing.Contractor = updated.Contractor;
        existing.Gip = updated.Gip;
        existing.WorkStatus = updated.WorkStatus;
        existing.WorkStart = updated.WorkStart;
        existing.Program = updated.Program;
    }
}
