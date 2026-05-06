using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectCharacteristicsViewModel : DirectoryViewModelBase<ProjectCharacteristic>
{
    public Func<ProjectCharacteristic?, Task>? OpenEditWindow { get; set; }

    public ProjectCharacteristicsViewModel(IDirectoryService<ProjectCharacteristic> service) : base(service)
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
}
