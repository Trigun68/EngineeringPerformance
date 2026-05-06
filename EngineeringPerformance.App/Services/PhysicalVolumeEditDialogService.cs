using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public interface IPhysicalVolumeEditDialogService
{
    void SetOwnerWindow(Window owner);

    /// <summary>
    /// Показывает модальное окно редактирования. Возвращает Id сохранённой записи
    /// (как для редактирования, так и для создания) или null, если пользователь
    /// отменил диалог.
    /// </summary>
    Task<int?> ShowEditDialogAsync(PhysicalVolume? item);
}

public sealed class PhysicalVolumeEditDialogService : IPhysicalVolumeEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PhysicalVolumeEditDialogService> _logger;
    private Window? _owner;

    public PhysicalVolumeEditDialogService(IServiceProvider serviceProvider, ILogger<PhysicalVolumeEditDialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<int?> ShowEditDialogAsync(PhysicalVolume? item)
    {
        if (_owner is null)
            return null;

        // Прогреваем кеши ДО показа окна. После startup-warmup и/или открытия
        // вкладки они обычно уже горячие и await завершается синхронно (~0 мс).
        // Холодный путь — один SQL под покрывающим индексом (~20 мс).
        // Так мы исключаем гонки на UI-потоке между await продолжением внутри
        // нестед message-pump диалога и параллельным await вкладочной навигации.
        await PrewarmLookupsAsync();

        await using var scope = _serviceProvider.CreateAsyncScope();

        var window = scope.ServiceProvider.GetRequiredService<PhysicalVolumeEditWindow>();
        var viewModel = (PhysicalVolumeEditViewModel)window.DataContext;

        window.Owner = _owner;

        viewModel.PrepareSync(
            item,
            result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        // Кеши гарантированно прогреты — LoadLookupsAsync завершается синхронно
        // в обычном случае. Отдельный CancellationTokenSource больше не нужен.
        try
        {
            await viewModel.LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load lookups for PhysicalVolume edit dialog");
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
            var pvCache = _serviceProvider.GetRequiredService<IDirectoryCache<PhysicalVolume>>();
            var pvTypeCache = _serviceProvider.GetRequiredService<IDirectoryCache<PhysicalVolumeType>>();
            var classifierCache = _serviceProvider.GetRequiredService<IDirectoryCache<IicWorkDirection>>();

            await Task.WhenAll(
                pvCache.GetAllAsync(),
                pvTypeCache.GetAllAsync(),
                classifierCache.GetAllAsync());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup pre-warm failed; dialog will load lazily");
        }
    }
}
