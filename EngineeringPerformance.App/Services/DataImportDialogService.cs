using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IDataImportDialogService
{
    void SetOwnerWindow(Window owner);
    Task<DataImportResult?> ShowImportDialogAsync(int projectId, int versionId, DataImportType dataType);
}

public sealed class DataImportDialogService : IDataImportDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public DataImportDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<DataImportResult?> ShowImportDialogAsync(int projectId, int versionId, DataImportType dataType)
    {
        if (_owner is null)
            return null;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<DataImportDialogViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<DataImportDialog>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        DataImportResult? result = null;
        window.Closed += (_, _) => result = viewModel.ImportResult;
        viewModel.Initialize(projectId, versionId, dataType, dialogResult => window.Dispatcher.Invoke(() => window.DialogResult = dialogResult));

        window.ShowDialog();
        return result;
    }
}
