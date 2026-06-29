using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;
using Serilog;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class EquipmentsViewModel : DirectoryViewModelBase<Equipment>
{
    private readonly IDirectoryService<Equipment> _equipmentService;
    private readonly IDirectoryCache<Equipment> _equipmentCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;

    public Func<Equipment?, Task>? OpenEditWindow { get; set; }

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

    private List<Equipment> _allLoadedItems = [];

    partial void OnSelectedAssessmentFilterChanged(bool? value)
    {
        ApplyFilters();
    }

    public EquipmentsViewModel(
        IDirectoryService<Equipment> service,
        IDirectoryCache<Equipment> equipmentCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache) : base(service)
    {
        _equipmentService = service;
        _equipmentCache = equipmentCache;
        _classifierCache = classifierCache;
        _unitCache = unitCache;
    }

    protected override void ApplyRefreshedItem(Equipment existing, Equipment updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Description = updated.Description;
        existing.ClassifierId = updated.ClassifierId;
        existing.Classifier = updated.Classifier;
        existing.ClassifierNameWithUnit = updated.ClassifierNameWithUnit;
        existing.UnitId = updated.UnitId;
        existing.Unit = updated.Unit;
        existing.UnitName = updated.UnitName;
        existing.IsAssessment = updated.IsAssessment;
        existing.Coefficient = updated.Coefficient;
        existing.NotifyRowRefreshed();
    }

    public override async Task LoadAsync()
    {
        var swLoad = Stopwatch.StartNew();
        if (OwnerWindow is not null)
        {
            await LoadingWindow.ShowAsync(OwnerWindow, async vm =>
            {
                vm.SetTitle("Загрузка: Номенклатура оборудования");
                vm.SetProgress(0, "Подключение к базе данных...");

                IsBusy = true;
                vm.SetProgress(30, "Загрузка справочников...");

                var swParallel = Stopwatch.StartNew();
                var classifiersTask = _classifierCache.GetAllAsync();
                var unitsTask = _unitCache.GetAllAsync();
                var itemsTask = ShowArchived
                    ? _equipmentService.GetAllAsync(true)
                    : LoadFromCacheAsync();

                await Task.WhenAll(classifiersTask, unitsTask, itemsTask);
                swParallel.Stop();

                vm.SetProgress(80, "Обработка данных...");
                _allLoadedItems = itemsTask.Result.ToList();

                var swEnrich = Stopwatch.StartNew();
                EnrichNavigations(_allLoadedItems, classifiersTask.Result, unitsTask.Result);
                swEnrich.Stop();

                var swApply = Stopwatch.StartNew();
                ApplyFilters();
                swApply.Stop();

                vm.SetProgress(100, "Готово");
                IsBusy = false;

                Log.Information("[PERF] EquipmentsVM.LoadAsync parallelFetch={P}ms enrich={E}ms applyFilters={A}ms total={T}ms rows={R}",
                    swParallel.ElapsedMilliseconds, swEnrich.ElapsedMilliseconds, swApply.ElapsedMilliseconds,
                    swLoad.ElapsedMilliseconds, _allLoadedItems.Count);
            });
        }
        else
        {
            await ReloadFromDatabaseCoreAsync();
        }
        swLoad.Stop();
    }

    public override async Task ReloadFromDatabaseAsync()
    {
        _equipmentCache.Invalidate();
        await ReloadFromDatabaseCoreAsync();
    }

    private async Task ReloadFromDatabaseCoreAsync()
    {
        IsBusy = true;
        try
        {
            var classifiersTask = _classifierCache.GetAllAsync();
            var unitsTask = _unitCache.GetAllAsync();
            var itemsTask = ShowArchived
                ? _equipmentService.GetAllAsync(true)
                : LoadFromCacheAsync();

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

    private async Task<IReadOnlyList<Equipment>> LoadFromCacheAsync()
        => await _equipmentCache.GetAllAsync();

    private static void EnrichNavigations(
        IReadOnlyList<Equipment> items,
        IReadOnlyList<IicWorkDirection> classifiers,
        IReadOnlyList<UnitOfMeasurement> units)
    {
        var classifierById = classifiers.ToDictionary(c => c.Id);
        var unitById = units.ToDictionary(u => u.Id);

        foreach (var item in items)
        {
            item.Classifier = item.ClassifierId.HasValue
                              && classifierById.TryGetValue(item.ClassifierId.Value, out var c)
                ? c
                : null;
            item.ClassifierNameWithUnit = item.Classifier is { } cl
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, classifierById)
                : string.Empty;
            item.Unit = item.UnitId.HasValue && unitById.TryGetValue(item.UnitId.Value, out var u)
                ? u
                : null;
            item.UnitName = item.Unit?.Name ?? string.Empty;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allLoadedItems.AsEnumerable();

        if (SelectedAssessmentFilter.HasValue)
        {
            filtered = filtered.Where(i => i.IsAssessment == SelectedAssessmentFilter.Value);
        }

        var filteredList = filtered.ToList();
        var preserveId = SelectedItem?.Id;

        Items = new ObservableCollection<Equipment>(filteredList);

        if (preserveId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(x => x.Id == preserveId.Value);
        }
    }

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

        var updated = await _equipmentService.GetByIdAsync(itemId);
        if (updated is null)
        {
            _allLoadedItems.RemoveAll(i => i.Id == itemId);
            var stale = Items.FirstOrDefault(i => i.Id == itemId);
            if (stale is not null) Items.Remove(stale);
            return;
        }

        var classifiers = await _classifierCache.GetAllAsync();
        var units = await _unitCache.GetAllAsync();
        EnrichNavigations(new[] { updated }, classifiers, units);

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

    private bool MatchesActiveFilters(Equipment item)
    {
        if (!ShowArchived && item.IsArchived) return false;
        if (SelectedAssessmentFilter.HasValue && item.IsAssessment != SelectedAssessmentFilter.Value) return false;
        return true;
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Номенклатура_оборудования.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var columns = new ReferenceExcelExporter.ColumnDef[]
        {
            new("Наименование", o => ((Equipment)o).Name, FixedWidth: 50),
            new("Ед. изм.",     o => ((Equipment)o).UnitName, FixedWidth: 12),
            new("Описание",     o => ((Equipment)o).Description, FixedWidth: 30),
            new("Классификатор",o => ((Equipment)o).ClassifierNameWithUnit, FixedWidth: 45),
            new("Коэффициент",  o => ((Equipment)o).Coefficient.ToString("G")),
            new("Оценка",       o => ((Equipment)o).IsAssessment?.ToString()),
            new("Архив",        o => ((Equipment)o).IsArchived.ToString()),
        };

        try
        {
            ReferenceExcelExporter.Export(
                "Номенклатура оборудования",
                columns,
                Items.Cast<object>(),
                dlg.FileName);
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
