using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectVersionPickerDialogService
{
    void SetOwnerWindow(Window owner);
    Task<int?> ShowPickerAsync(int projectId, string projectName, AnalysisDirection direction);
    Task<int?> ShowPickerAsync(int projectId, string projectName);
}

public sealed class ProjectVersionPickerDialogService : IProjectVersionPickerDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectVersionPickerDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<int?> ShowPickerAsync(int projectId, string projectName, AnalysisDirection direction)
    {
        if (_owner is null) return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectVersionPickerDialogViewModel>();
        var win = scope.ServiceProvider.GetRequiredService<ProjectVersionPickerDialog>();
        win.DataContext = vm;
        win.Owner = _owner;
        return await win.ShowPickerAsync(_owner, projectId, projectName, direction);
    }

    public async Task<int?> ShowPickerAsync(int projectId, string projectName)
    {
        if (_owner is null) return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectVersionPickerDialogViewModel>();
        var win = scope.ServiceProvider.GetRequiredService<ProjectVersionPickerDialog>();
        win.DataContext = vm;
        win.Owner = _owner;
        return await win.ShowPickerAsync(_owner, projectId, projectName, null);
    }
}
