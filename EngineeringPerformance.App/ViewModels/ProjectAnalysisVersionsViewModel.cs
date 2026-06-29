using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectAnalysisVersionsViewModel : DirectoryViewModelBase<ProjectAnalysisVersion>
{
    public Func<ProjectAnalysisVersion?, int?, Task>? OpenEditWindow { get; set; }

    public ProjectAnalysisVersionsViewModel(IDirectoryService<ProjectAnalysisVersion> service) : base(service)
    {
    }

    protected override void ApplyRefreshedItem(ProjectAnalysisVersion existing, ProjectAnalysisVersion updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.ProjectId = updated.ProjectId;
        existing.VersionName = updated.VersionName;
        existing.Description = updated.Description;
        existing.Direction = updated.Direction;
        existing.PhysicalVolumeTypeId = updated.PhysicalVolumeTypeId;
        existing.PhysicalVolumeType = updated.PhysicalVolumeType;
        existing.Project = updated.Project;
    }

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
            await OpenEditWindow(null, null);
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null || OpenEditWindow is null) return;

        await OpenEditWindow(SelectedItem, null);
    }
}
