using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public interface IIicWorkDirectionEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(IicWorkDirection? item);
}

public sealed class IicWorkDirectionEditDialogService : IIicWorkDirectionEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IicWorkDirectionEditDialogService> _logger;
    private Window? _owner;

    public IicWorkDirectionEditDialogService(IServiceProvider serviceProvider, ILogger<IicWorkDirectionEditDialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(IicWorkDirection? item)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var window = scope.ServiceProvider.GetRequiredService<IicWorkDirectionEditWindow>();
        var viewModel = (IicWorkDirectionEditViewModel)window.DataContext;

        window.Owner = _owner;

        viewModel.PrepareSync(
            item,
            result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        using var loadCts = new CancellationTokenSource();
        window.Closed += (_, _) => loadCts.Cancel();

        var loadTask = LoadLookupsSafelyAsync(viewModel, loadCts.Token);

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        window.ShowDialog();

        try
        {
            await loadTask;
        }
        catch (OperationCanceledException)
        {
        }

        return dialogResult == true;
    }

    private async Task LoadLookupsSafelyAsync(
        IicWorkDirectionEditViewModel viewModel,
        CancellationToken cancellationToken)
    {
        try
        {
            await viewModel.LoadLookupsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load lookups for IicWorkDirection edit dialog");
        }
    }
}
