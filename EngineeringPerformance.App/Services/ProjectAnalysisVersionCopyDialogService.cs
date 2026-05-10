using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectAnalysisVersionCopyDialogService
{
    void SetOwnerWindow(Window owner);

    /// <summary>
    /// Показывает мини-диалог копирования версии. Возвращает Id новой версии при успехе либо null при отмене/ошибке.
    /// </summary>
    Task<int?> ShowAsync(ProjectAnalysisVersion source);
}

public sealed class ProjectAnalysisVersionCopyDialogService : IProjectAnalysisVersionCopyDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectAnalysisVersionCopyDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public Task<int?> ShowAsync(ProjectAnalysisVersion source)
    {
        var viewModel = _serviceProvider.GetRequiredService<ProjectAnalysisVersionCopyViewModel>();
        var window = _serviceProvider.GetRequiredService<ProjectAnalysisVersionCopyDialog>();

        int? createdId = null;
        viewModel.Prepare(source, newId =>
        {
            createdId = newId;
            window.Dispatcher.Invoke(() => window.DialogResult = newId.HasValue);
        });

        window.DataContext = viewModel;
        window.Owner = _owner ?? Application.Current.MainWindow;
        window.ShowDialog();

        return Task.FromResult(createdId);
    }
}
