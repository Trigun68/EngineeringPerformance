using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface ILaborIntensityEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(LaborIntensity? item);
}

public sealed class LaborIntensityEditDialogService : ILaborIntensityEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public LaborIntensityEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(LaborIntensity? item)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<LaborIntensityEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<LaborIntensityEditWindow>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        await viewModel.InitializeAsync(item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        return dialogResult == true;
    }
}
