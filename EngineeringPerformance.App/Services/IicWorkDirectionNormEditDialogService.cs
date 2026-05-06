using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public sealed class IicWorkDirectionNormEditDialogService : IIicWorkDirectionNormEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IicWorkDirectionNormEditDialogService> _logger;
    private Window? _owner;

    public IicWorkDirectionNormEditDialogService(IServiceProvider serviceProvider, ILogger<IicWorkDirectionNormEditDialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<bool> ShowEditDialogAsync(
        IicWorkDirectionNorm? item = null,
        int? preselectedWorkDirectionId = null)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var window = scope.ServiceProvider.GetRequiredService<IicWorkDirectionNormEditWindow>();
        var viewModel = (IicWorkDirectionNormEditViewModel)window.DataContext;

        window.Owner = _owner;

        viewModel.PrepareSync(
            item,
            preselectedWorkDirectionId,
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
        IicWorkDirectionNormEditViewModel viewModel,
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
            _logger.LogError(ex, "Failed to load lookups for IicWorkDirectionNorm edit dialog");
        }
    }
}
