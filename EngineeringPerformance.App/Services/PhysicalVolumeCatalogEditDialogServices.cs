using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public interface IPhysicalVolumeKfoEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<int?> ShowEditDialogAsync(PhysicalVolumeKfo? item);
}

public interface IPhysicalVolumeBerEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<int?> ShowEditDialogAsync(PhysicalVolumeBer? item);
}

public sealed class PhysicalVolumeKfoEditDialogService(
    IServiceProvider serviceProvider,
    ILogger<PhysicalVolumeKfoEditDialogService> logger) : IPhysicalVolumeKfoEditDialogService
{
    private Window? _owner;

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<int?> ShowEditDialogAsync(PhysicalVolumeKfo? item)
    {
        if (_owner is null) return null;

        await PrewarmLookupsAsync<PhysicalVolumeKfo>();

        await using var scope = serviceProvider.CreateAsyncScope();
        var window = scope.ServiceProvider.GetRequiredService<PhysicalVolumeKfoEditWindow>();
        var viewModel = (PhysicalVolumeKfoEditViewModel)window.DataContext;
        window.Owner = _owner;

        viewModel.PrepareSync(item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));
        try { await viewModel.LoadLookupsAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Failed to load lookups for PhysicalVolume KFO edit dialog"); }

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        window.ShowDialog();
        return dialogResult == true ? viewModel.EntityId : null;
    }

    private async Task PrewarmLookupsAsync<T>() where T : DirectoryItem
    {
        try
        {
            await Task.WhenAll(
                serviceProvider.GetRequiredService<IDirectoryCache<T>>().GetAllAsync(),
                serviceProvider.GetRequiredService<IDirectoryCache<IicWorkDirection>>().GetAllAsync(),
                serviceProvider.GetRequiredService<IDirectoryCache<UnitOfMeasurement>>().GetAllAsync());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lookup pre-warm failed; dialog will load lazily");
        }
    }
}

public sealed class PhysicalVolumeBerEditDialogService(
    IServiceProvider serviceProvider,
    ILogger<PhysicalVolumeBerEditDialogService> logger) : IPhysicalVolumeBerEditDialogService
{
    private Window? _owner;

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<int?> ShowEditDialogAsync(PhysicalVolumeBer? item)
    {
        if (_owner is null) return null;

        await PrewarmLookupsAsync<PhysicalVolumeBer>();

        await using var scope = serviceProvider.CreateAsyncScope();
        var window = scope.ServiceProvider.GetRequiredService<PhysicalVolumeBerEditWindow>();
        var viewModel = (PhysicalVolumeBerEditViewModel)window.DataContext;
        window.Owner = _owner;

        viewModel.PrepareSync(item, result => window.Dispatcher.Invoke(() => window.DialogResult = result));
        try { await viewModel.LoadLookupsAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Failed to load lookups for PhysicalVolume BER edit dialog"); }

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        window.ShowDialog();
        return dialogResult == true ? viewModel.EntityId : null;
    }

    private async Task PrewarmLookupsAsync<T>() where T : DirectoryItem
    {
        try
        {
            await Task.WhenAll(
                serviceProvider.GetRequiredService<IDirectoryCache<T>>().GetAllAsync(),
                serviceProvider.GetRequiredService<IDirectoryCache<IicWorkDirection>>().GetAllAsync(),
                serviceProvider.GetRequiredService<IDirectoryCache<UnitOfMeasurement>>().GetAllAsync());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lookup pre-warm failed; dialog will load lazily");
        }
    }
}
