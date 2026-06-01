using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public interface IEquipmentEditDialogService
{
    void SetOwnerWindow(Window owner);

    Task<int?> ShowEditDialogAsync(Equipment? item);
}

public sealed class EquipmentEditDialogService : IEquipmentEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EquipmentEditDialogService> _logger;
    private Window? _owner;

    public EquipmentEditDialogService(IServiceProvider serviceProvider, ILogger<EquipmentEditDialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<int?> ShowEditDialogAsync(Equipment? item)
    {
        if (_owner is null)
            return null;

        await PrewarmLookupsAsync();

        await using var scope = _serviceProvider.CreateAsyncScope();

        var window = scope.ServiceProvider.GetRequiredService<EquipmentEditWindow>();
        var viewModel = (EquipmentEditViewModel)window.DataContext;

        window.Owner = _owner;

        viewModel.PrepareSync(
            item,
            result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        try
        {
            await viewModel.LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load lookups for Equipment edit dialog");
        }

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        window.ShowDialog();

        return dialogResult == true ? viewModel.EntityId : null;
    }

    private async Task PrewarmLookupsAsync()
    {
        try
        {
            var equipmentCache = _serviceProvider.GetRequiredService<IDirectoryCache<Equipment>>();
            var classifierCache = _serviceProvider.GetRequiredService<IDirectoryCache<IicWorkDirection>>();

            await Task.WhenAll(
                equipmentCache.GetAllAsync(),
                classifierCache.GetAllAsync());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup pre-warm failed; dialog will load lazily");
        }
    }
}
