using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;
using Serilog;

namespace EngineeringPerformance.App.ViewModels;

/// <summary>Общая логика справочника номенклатуры КФО/БЭР без фильтра по виду.</summary>
public abstract partial class PhysicalVolumesCatalogViewModelBase<T> : DirectoryViewModelBase<T>
    where T : DirectoryItem, new()
{
    private readonly IDirectoryService<T> _pvService;
    private readonly IDirectoryCache<T> _pvCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;

    protected abstract string LoadingTitle { get; }
    protected abstract string ExportSheetTitle { get; }
    protected abstract string ExportFileName { get; }

    protected abstract void ApplyCatalogFields(T existing, T updated);
    protected abstract void EnrichItem(T item, IReadOnlyDictionary<int, IicWorkDirection> classifiers, IReadOnlyDictionary<int, UnitOfMeasurement> units);
    protected abstract IEnumerable<(string Header, Func<object, object?> Value, int? FixedWidth)> ExportColumns { get; }

    public Func<T?, Task>? OpenEditWindow { get; set; }

    [ObservableProperty]
    private bool? _selectedAssessmentFilter;

    [ObservableProperty]
    private string _selectedAssessmentFilterOption = string.Empty;

    public ObservableCollection<AssessmentFilterOption> AssessmentFilterOptions { get; } =
    [
        new AssessmentFilterOption { Value = "true", Display = "Да" },
        new AssessmentFilterOption { Value = "false", Display = "Нет" },
    ];

    partial void OnSelectedAssessmentFilterOptionChanged(string value)
    {
        SelectedAssessmentFilter = value switch
        {
            "true" => true,
            "false" => false,
            _ => null,
        };
    }

    private List<T> _allLoadedItems = [];

    partial void OnSelectedAssessmentFilterChanged(bool? value) => ApplyFilters();

    protected PhysicalVolumesCatalogViewModelBase(
        IDirectoryService<T> service,
        IDirectoryCache<T> pvCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache) : base(service)
    {
        _pvService = service;
        _pvCache = pvCache;
        _classifierCache = classifierCache;
        _unitCache = unitCache;
    }

    protected override void ApplyRefreshedItem(T existing, T updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        ApplyCatalogFields(existing, updated);
        if (existing is PhysicalVolumeKfo kfo && updated is PhysicalVolumeKfo kfoUpd)
            kfo.NotifyRowRefreshed();
        else if (existing is PhysicalVolumeBer ber && updated is PhysicalVolumeBer berUpd)
            ber.NotifyRowRefreshed();
    }

    public override async Task LoadAsync()
    {
        var swLoad = Stopwatch.StartNew();
        if (OwnerWindow is not null)
        {
            await LoadingWindow.ShowAsync(OwnerWindow, async vm =>
            {
                vm.SetTitle(LoadingTitle);
                vm.SetProgress(0, "Подключение к базе данных...");
                IsBusy = true;
                vm.SetProgress(30, "Загрузка справочников...");

                var classifiersTask = _classifierCache.GetAllAsync();
                var unitsTask = _unitCache.GetAllAsync();
                var itemsTask = ShowArchived ? _pvService.GetAllAsync(true) : LoadFromCacheAsync();
                await Task.WhenAll(classifiersTask, unitsTask, itemsTask);

                vm.SetProgress(80, "Обработка данных...");
                _allLoadedItems = itemsTask.Result.ToList();
                EnrichNavigations(_allLoadedItems, classifiersTask.Result, unitsTask.Result);
                ApplyFilters();
                vm.SetProgress(100, "Готово");
                IsBusy = false;

                Log.Information("[PERF] {Type}.LoadAsync total={T}ms rows={R}",
                    typeof(T).Name, swLoad.ElapsedMilliseconds, _allLoadedItems.Count);
            });
        }
        else
        {
            await ReloadFromDatabaseCoreAsync();
        }
    }

    public override async Task ReloadFromDatabaseAsync()
    {
        _pvCache.Invalidate();
        await ReloadFromDatabaseCoreAsync();
    }

    private async Task ReloadFromDatabaseCoreAsync()
    {
        IsBusy = true;
        try
        {
            var classifiersTask = _classifierCache.GetAllAsync();
            var unitsTask = _unitCache.GetAllAsync();
            var itemsTask = ShowArchived ? _pvService.GetAllAsync(true) : LoadFromCacheAsync();
            await Task.WhenAll(classifiersTask, unitsTask, itemsTask);
            _allLoadedItems = itemsTask.Result.ToList();
            EnrichNavigations(_allLoadedItems, classifiersTask.Result, unitsTask.Result);
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<IReadOnlyList<T>> LoadFromCacheAsync() => await _pvCache.GetAllAsync();

    private void EnrichNavigations(
        IReadOnlyList<T> items,
        IReadOnlyList<IicWorkDirection> classifiers,
        IReadOnlyList<UnitOfMeasurement> units)
    {
        var classifierById = classifiers.ToDictionary(c => c.Id);
        var unitById = units.ToDictionary(u => u.Id);
        foreach (var item in items)
            EnrichItem(item, classifierById, unitById);
    }

    private void ApplyFilters()
    {
        var filtered = _allLoadedItems.AsEnumerable();
        if (SelectedAssessmentFilter.HasValue)
            filtered = filtered.Where(MatchesAssessmentFilter);

        var filteredList = filtered.ToList();
        var preserveId = SelectedItem?.Id;
        Items = new ObservableCollection<T>(filteredList);
        if (preserveId.HasValue)
            SelectedItem = Items.FirstOrDefault(x => x.Id == preserveId.Value);
    }

    protected abstract bool MatchesAssessmentFilter(T item);

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
            await OpenEditWindow(null);
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null || OpenEditWindow is null) return;
        await OpenEditWindow(SelectedItem);
    }

    public async Task RefreshSingleItemAsync(int itemId)
    {
        if (itemId <= 0) return;

        var updated = await _pvService.GetByIdAsync(itemId);
        if (updated is null)
        {
            _allLoadedItems.RemoveAll(i => i.Id == itemId);
            var stale = Items.FirstOrDefault(i => i.Id == itemId);
            if (stale is not null) Items.Remove(stale);
            return;
        }

        var classifiersTask = _classifierCache.GetAllAsync();
        var unitsTask = _unitCache.GetAllAsync();
        await Task.WhenAll(classifiersTask, unitsTask);
        EnrichNavigations(new[] { updated }, classifiersTask.Result, unitsTask.Result);

        var passesFilter = MatchesActiveFilters(updated);
        var existingFlat = _allLoadedItems.FirstOrDefault(i => i.Id == itemId);
        var existingInItems = Items.FirstOrDefault(i => i.Id == itemId);

        if (existingFlat is null)
        {
            _allLoadedItems.Add(updated);
            if (passesFilter) Items.Add(updated);
            return;
        }

        var flatIndex = _allLoadedItems.IndexOf(existingFlat);
        _allLoadedItems[flatIndex] = updated;

        if (existingInItems is null)
        {
            if (passesFilter) Items.Add(updated);
            return;
        }

        if (!passesFilter)
        {
            Items.Remove(existingInItems);
            return;
        }

        ApplyRefreshedItem(existingInItems, updated);
    }

    private bool MatchesActiveFilters(T item)
    {
        if (!ShowArchived && item.IsArchived) return false;
        if (SelectedAssessmentFilter.HasValue && !MatchesAssessmentFilter(item)) return false;
        return true;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = ExportFileName,
        };
        if (dlg.ShowDialog() != true) return;

        var columns = ExportColumns
            .Select(c => new ReferenceExcelExporter.ColumnDef(c.Header, c.Value, c.FixedWidth))
            .ToArray();

        try
        {
            ReferenceExcelExporter.Export(ExportSheetTitle, columns, Items.Cast<object>(), dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
