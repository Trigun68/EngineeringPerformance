using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(Project? item);
}

public sealed class ProjectEditDialogService : IProjectEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(Project? item)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<ProjectEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<ProjectEditWindow>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        await viewModel.InitializeAsync(item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        return dialogResult == true;
    }
}
