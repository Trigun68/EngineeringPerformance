using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectPickerDialogService
{
    void SetOwnerWindow(Window owner);
    Task<int?> ShowPickerAsync();
}

public sealed class ProjectPickerDialogService : IProjectPickerDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectPickerDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<int?> ShowPickerAsync()
    {
        if (_owner is null) return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectPickerDialogViewModel>();
        var win = scope.ServiceProvider.GetRequiredService<ProjectPickerDialog>();
        win.DataContext = vm;
        win.Owner = _owner;
        return await win.ShowPickerAsync(_owner);
    }
}
