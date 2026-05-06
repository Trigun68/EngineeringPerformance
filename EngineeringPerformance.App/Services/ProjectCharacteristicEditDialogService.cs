using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectCharacteristicEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(ProjectCharacteristic? item);
}

public sealed class ProjectCharacteristicEditDialogService : IProjectCharacteristicEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectCharacteristicEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(ProjectCharacteristic? item)
    {
        if (_owner is null) return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicEditWindow>();

        // Подгружаем варианты целиком до открытия окна, чтобы грид сразу
        // показал текущий набор без визуального скачка.
        ProjectCharacteristic? withOptions = null;
        if (item is not null)
        {
            var svc = scope.ServiceProvider.GetRequiredService<ProjectCharacteristicService>();
            withOptions = await svc.GetByIdAsync(item.Id);
        }

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        viewModel.Initialize(withOptions ?? item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        return dialogResult == true;
    }
}
