using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectPhysicalVolumeCreateDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId);
}

public sealed class ProjectPhysicalVolumeCreateDialogService : IProjectPhysicalVolumeCreateDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectPhysicalVolumeCreateDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<bool> ShowAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        var viewModel = _serviceProvider.GetRequiredService<ProjectPhysicalVolumeCreateViewModel>();
        viewModel.Prepare(forCopy, source, projectId, versionId);

        var window = _serviceProvider.GetRequiredService<ProjectPhysicalVolumeCreateDialog>();
        window.DataContext = viewModel;
        window.Owner = _owner ?? Application.Current.MainWindow;

        await viewModel.LoadLookupsAsync();
        return window.ShowDialog() == true;
    }
}
