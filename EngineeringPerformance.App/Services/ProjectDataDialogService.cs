using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectDataDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowAsync(Project project);
}

public sealed class ProjectDataDialogService : IProjectDataDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectDataDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public Task<bool> ShowAsync(Project project)
    {
        if (_owner is null) return Task.FromResult(false);

        var vm = _serviceProvider.GetRequiredService<ProjectDataDialogViewModel>();
        var win = _serviceProvider.GetRequiredService<ProjectDataDialog>();
        win.Owner = _owner;
        win.DataContext = vm;
        vm.Initialize(project, result => win.Dispatcher.Invoke(() => win.DialogResult = result));
        return Task.FromResult(win.ShowDialog() == true);
    }
}
