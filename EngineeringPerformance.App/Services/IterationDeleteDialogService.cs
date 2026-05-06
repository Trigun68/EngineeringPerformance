using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IIterationDeleteDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowDeleteDialogAsync(int projectId, int versionId, DataImportType dataType);
}

public sealed class IterationDeleteDialogService : IIterationDeleteDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public IterationDeleteDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task<bool> ShowDeleteDialogAsync(int projectId, int versionId, DataImportType dataType)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<IterationDeleteDialogViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<IterationDeleteDialog>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        await viewModel.InitializeAsync(projectId, versionId, dataType, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        // Закрытие крестиком окна оставляет DialogResult = null. Если в этом сеансе
        // что-то всё же удалили — считаем результат успешным, чтобы вызывающая
        // сторона обновила таблицу (как при штатном «Отмена» после удаления).
        return dialogResult == true || viewModel.AnyDeleted;
    }
}
