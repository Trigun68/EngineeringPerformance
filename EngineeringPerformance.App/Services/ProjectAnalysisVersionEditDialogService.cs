using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public interface IProjectAnalysisVersionEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(ProjectAnalysisVersion? item, int? preselectedProjectId = null, AnalysisDirection? direction = null);
}

public sealed class ProjectAnalysisVersionEditDialogService : IProjectAnalysisVersionEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectAnalysisVersionEditDialogService> _logger;
    private Window? _owner;

    public ProjectAnalysisVersionEditDialogService(IServiceProvider serviceProvider, ILogger<ProjectAnalysisVersionEditDialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(ProjectAnalysisVersion? item, int? preselectedProjectId = null, AnalysisDirection? direction = null)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var window = scope.ServiceProvider.GetRequiredService<ProjectAnalysisVersionEditWindow>();
        var viewModel = (ProjectAnalysisVersionEditViewModel)window.DataContext;

        window.Owner = _owner;

        viewModel.PrepareSync(
            item,
            preselectedProjectId,
            direction,
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
        ProjectAnalysisVersionEditViewModel viewModel,
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
            _logger.LogError(ex, "Failed to load lookups for ProjectAnalysisVersion edit dialog");
        }
    }
}
