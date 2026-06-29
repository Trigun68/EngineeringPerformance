using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectEquipmentCreateDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId);
    Task<bool> ShowAsync(bool forCopy, FactDetailRow? source, int projectId, int versionId);
}

public sealed class ProjectEquipmentCreateDialogService : IProjectEquipmentCreateDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectEquipmentCreateDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<bool> ShowAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        var viewModel = _serviceProvider.GetRequiredService<ProjectEquipmentCreateViewModel>();
        viewModel.Prepare(forCopy, source, projectId, versionId);
        return await ShowInternalAsync(viewModel);
    }

    public async Task<bool> ShowAsync(bool forCopy, FactDetailRow? source, int projectId, int versionId)
    {
        var viewModel = _serviceProvider.GetRequiredService<ProjectEquipmentCreateViewModel>();
        viewModel.Prepare(forCopy, source, projectId, versionId);
        return await ShowInternalAsync(viewModel);
    }

    private async Task<bool> ShowInternalAsync(ProjectEquipmentCreateViewModel viewModel)
    {
        var window = _serviceProvider.GetRequiredService<ProjectEquipmentCreateDialog>();
        window.DataContext = viewModel;
        window.Owner = _owner ?? Application.Current.MainWindow;

        await viewModel.LoadLookupsAsync();
        return window.ShowDialog() == true;
    }
}
