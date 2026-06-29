using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectSelectionDialogService
{
    void SetOwnerWindow(Window owner);
    Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync();
    Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync(AnalysisDirection direction);
}

public sealed class ProjectSelectionDialogService : IProjectSelectionDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectSelectionDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync()
    {
        return await ShowSelectionDialogAsync(AnalysisDirection.Fact);
    }

    public async Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync(AnalysisDirection direction)
    {
        if (_owner is null)
            return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<ProjectSelectionDialogViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<ProjectSelectionDialog>();
        var versionEditDialog = _serviceProvider.GetRequiredService<IProjectAnalysisVersionEditDialogService>();
        versionEditDialog.SetOwnerWindow(_owner);

        viewModel.DirectionFilter = direction;
        window.Owner = _owner;
        window.DataContext = viewModel;

        return await window.ShowSelectionDialogAsync(_owner);
    }
}
