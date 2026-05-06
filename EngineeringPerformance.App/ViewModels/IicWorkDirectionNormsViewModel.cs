using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Models;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IicWorkDirectionNormsViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IDateFilteredRegisterService<IicWorkDirectionNorm> _service;
    private readonly IDirectoryService<IicWorkDirection> _directionService;
    private CancellationTokenSource? _loadCts;

    public Window? OwnerWindow { get; set; }

    public Func<IicWorkDirectionNorm?, int?, Task>? OpenEditWindow { get; set; }

    [ObservableProperty]
    private ObservableCollection<IicWorkDirectionNormDisplay> _items = [];

    [ObservableProperty]
    private IicWorkDirectionNormDisplay? _selectedItem;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private bool _isBusy;

    private IReadOnlyList<IicWorkDirection> _allDirections = [];

    public IicWorkDirectionNormsViewModel(
        IDateFilteredRegisterService<IicWorkDirectionNorm> service,
        IDirectoryService<IicWorkDirection> directionService)
    {
        _service = service;
        _directionService = directionService;
    }

    /// <summary>
    /// Сбрасывает состояние singleton VM. Вызывается из <see cref="App"/> при закрытии главного окна (A3).
    /// </summary>
    public void Reset()
    {
        try { _loadCts?.Cancel(); } catch { /* ignore */ }
        _loadCts = null;
        Items = [];
        SelectedItem = null;
        SelectedDate = DateTime.Today;
        IsBusy = false;
        OwnerWindow = null;
        _allDirections = [];
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadSafeAsync();
    }

    private async Task LoadSafeAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            await LoadInternalAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAsync error: {ex.Message}");
        }
    }

    public async Task LoadAsync()
    {
        await LoadInternalAsync();
    }

    private async Task LoadInternalAsync()
    {
        if (OwnerWindow is not null)
        {
            await LoadingWindow.ShowAsync(OwnerWindow, async vm =>
            {
                vm.SetTitle("Загрузка: Нормы групп физ. объёмов (IIC)");
                vm.SetProgress(0, "Подключение к базе данных...");

                IsBusy = true;
                vm.SetProgress(30, "Загрузка данных...");

                var norms = await _service.GetByDateAsync(SelectedDate);
                _allDirections = await _directionService.GetAllAsync(true);

                vm.SetProgress(80, "Обработка данных...");
                BuildTree(norms, _allDirections);

                vm.SetProgress(100, "Готово");
                IsBusy = false;
            });
        }
        else
        {
            IsBusy = true;
            try
            {
                var norms = await _service.GetByDateAsync(SelectedDate);
                _allDirections = await _directionService.GetAllAsync(true);
                BuildTree(norms, _allDirections);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private void BuildTree(IReadOnlyList<IicWorkDirectionNorm> norms, IReadOnlyList<IicWorkDirection> directions)
    {
        var normByDirectionId = norms.ToDictionary(n => n.WorkDirectionId, n => n);
        var byId = directions.ToDictionary(d => d.Id);
        var displayItems = new List<IicWorkDirectionNormDisplay>();

        foreach (var dir in directions)
        {
            var hasNorm = normByDirectionId.TryGetValue(dir.Id, out var norm);

            var display = new IicWorkDirectionNormDisplay
            {
                Id = dir.Id,
                NormId = hasNorm ? norm!.Id : 0,
                Code = dir.Code,
                Name = dir.Name,
                ParentId = dir.ParentId,
                Performance = hasNorm ? norm!.Performance : null,
                Estimate = hasNorm ? norm!.Estimate : null,
                StandardDeviation = hasNorm ? norm!.StandardDeviation : null,
                UnitName = IicWorkDirectionDisplayUnit.ResolveUnitName(dir, byId),
                EffectiveFrom = hasNorm ? norm!.EffectiveFrom : DateTime.MinValue,
                EffectiveTo = hasNorm ? norm!.EffectiveTo : null,
            };

            displayItems.Add(display);
        }

        var ordered = displayItems.OrderBy(d => d.ParentId).ThenBy(d => d.Code).ToList();
        var freshById = ordered.ToDictionary(x => x.Id);
        var preserveId = SelectedItem?.Id;

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            var row = Items[i];
            if (!freshById.TryGetValue(row.Id, out var src))
            {
                Items.RemoveAt(i);
                continue;
            }

            row.CopyValuesFrom(src);
        }

        var existingIds = Items.Select(x => x.Id).ToHashSet();
        foreach (var src in ordered)
        {
            if (!existingIds.Contains(src.Id))
            {
                Items.Add(src);
            }
        }

        if (preserveId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(x => x.Id == preserveId.Value);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadSafeAsync();

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
        {
            var preselectedId = SelectedItem?.Id;
            await OpenEditWindow(null, preselectedId);
            await LoadSafeAsync();
        }
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null || OpenEditWindow is null) return;
        if (SelectedItem.NormId <= 0) return;

        var norm = await _service.GetByIdAsync(SelectedItem.NormId);
        if (norm is null) return;

        await OpenEditWindow(norm, null);
        await LoadSafeAsync();
    }

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (SelectedItem is null || SelectedItem.NormId <= 0) return;

        await _service.DeleteAsync(SelectedItem.NormId);
        await LoadSafeAsync();
    }
}
