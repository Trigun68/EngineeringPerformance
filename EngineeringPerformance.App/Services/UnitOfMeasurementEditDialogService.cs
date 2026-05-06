using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IUnitOfMeasurementEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(UnitOfMeasurement? item);
}

public sealed class UnitOfMeasurementEditDialogService : IUnitOfMeasurementEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public UnitOfMeasurementEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(UnitOfMeasurement? item)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<UnitOfMeasurementEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<UnitOfMeasurementEditWindow>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        await viewModel.InitializeAsync(item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        return dialogResult == true;
    }
}
