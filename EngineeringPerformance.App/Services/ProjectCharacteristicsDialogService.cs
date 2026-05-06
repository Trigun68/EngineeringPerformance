using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectCharacteristicsDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowAsync(int projectId);
}

public sealed class ProjectCharacteristicsDialogService : IProjectCharacteristicsDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProjectCharacteristicSelectionEditDialogService _innerEditDialog;
    private Window? _owner;

    public ProjectCharacteristicsDialogService(
        IServiceProvider serviceProvider,
        IProjectCharacteristicSelectionEditDialogService innerEditDialog)
    {
        _serviceProvider = serviceProvider;
        _innerEditDialog = innerEditDialog;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<bool> ShowAsync(int projectId)
    {
        if (_owner is null) return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicsDialogViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicsDialog>();

        window.Owner = _owner;
        window.DataContext = vm;
        // Под-диалог редактирования выбора должен открываться поверх этого окна,
        // а не главного — иначе при закрытии модального уйдёт фокус.
        _innerEditDialog.SetOwnerWindow(window);

        bool? result = null;
        window.Closed += (_, _) => result = window.DialogResult;
        await vm.InitializeAsync(projectId, r => window.Dispatcher.Invoke(() => window.DialogResult = r));

        window.ShowDialog();
        return result == true;
    }
}

public interface IProjectCharacteristicSelectionEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<ProjectCharacteristicSelectionResult?> ShowAsync(
        ProjectCharacteristicSelectionRowVm? existing,
        IReadOnlyList<int> excludedCharacteristicIds);
}

public sealed class ProjectCharacteristicSelectionEditDialogService : IProjectCharacteristicSelectionEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectCharacteristicSelectionEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<ProjectCharacteristicSelectionResult?> ShowAsync(
        ProjectCharacteristicSelectionRowVm? existing,
        IReadOnlyList<int> excludedCharacteristicIds)
    {
        if (_owner is null) return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicSelectionEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicSelectionEditWindow>();

        window.Owner = _owner;
        window.DataContext = vm;

        ProjectCharacteristicSelectionResult? result = null;
        await vm.InitializeAsync(existing, excludedCharacteristicIds, r =>
        {
            result = r;
            window.Dispatcher.Invoke(() =>
            {
                window.DialogResult = r is not null;
            });
        });

        window.ShowDialog();
        return result;
    }
}
