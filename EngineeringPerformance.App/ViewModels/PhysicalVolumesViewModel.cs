using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Serilog;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PhysicalVolumesViewModel : DirectoryViewModelBase<PhysicalVolume>
{
    private readonly IDirectoryService<PhysicalVolume> _pvService;
    private readonly IDirectoryCache<PhysicalVolume> _pvCache;
    private readonly IDirectoryCache<PhysicalVolumeType> _volumeTypeCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;

    public Func<PhysicalVolume?, Task>? OpenEditWindow { get; set; }

    [ObservableProperty]
    private int? _selectedVolumeTypeIdFilter;

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

    [ObservableProperty]
    private ObservableCollection<PhysicalVolumeType> _volumeTypes = [];

    private List<PhysicalVolume> _allLoadedItems = [];

    partial void OnSelectedVolumeTypeIdFilterChanged(int? value)
    {
        ApplyFilters();
    }

    partial void OnSelectedAssessmentFilterChanged(bool? value)
    {
        ApplyFilters();
    }

    public PhysicalVolumesViewModel(
        IDirectoryService<PhysicalVolume> service,
        IDirectoryCache<PhysicalVolume> pvCache,
        IDirectoryCache<PhysicalVolumeType> volumeTypeCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache) : base(service)
    {
        _pvService = service;
        _pvCache = pvCache;
        _volumeTypeCache = volumeTypeCache;
        _classifierCache = classifierCache;
        _unitCache = unitCache;
    }

    protected override void ApplyRefreshedItem(PhysicalVolume existing, PhysicalVolume updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Code = updated.Code;
        existing.Description = updated.Description;
        existing.VolumeTypeId = updated.VolumeTypeId;
        existing.VolumeType = updated.VolumeType;
        existing.ClassifierId = updated.ClassifierId;
        existing.Classifier = updated.Classifier;
        existing.ClassifierNameWithUnit = updated.ClassifierNameWithUnit;
        existing.UnitId = updated.UnitId;
        existing.Unit = updated.Unit;
        existing.IsAssessment = updated.IsAssessment;
        existing.DocumentSet = updated.DocumentSet;
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
                vm.SetTitle("Загрузка: Номенклатура физических объёмов");
                vm.SetProgress(0, "Подключение к базе данных...");

                IsBusy = true;
                vm.SetProgress(30, "Загрузка справочников...");

                var swParallel = Stopwatch.StartNew();
                var typesTask = _volumeTypeCache.GetAllAsync();
                var classifiersTask = _classifierCache.GetAllAsync();
                var unitsTask = _unitCache.GetAllAsync();
                var itemsTask = ShowArchived
                    ? _pvService.GetAllAsync(true)
                    : LoadFromCacheAsync();

                await Task.WhenAll(typesTask, classifiersTask, unitsTask, itemsTask);
                swParallel.Stop();

                vm.SetProgress(80, "Обработка данных...");
                _allLoadedItems = itemsTask.Result.ToList();

                var swEnrich = Stopwatch.StartNew();
                EnrichNavigations(_allLoadedItems, typesTask.Result, classifiersTask.Result, unitsTask.Result);
                swEnrich.Stop();

                var swApply = Stopwatch.StartNew();
                ApplyFilters();
                swApply.Stop();

                SyncVolumeTypes(typesTask.Result);

                vm.SetProgress(100, "Готово");
                IsBusy = false;

                Log.Information("[PERF] PhysicalVolumesVM.LoadAsync parallelFetch={P}ms enrich={E}ms applyFilters={A}ms total={T}ms rows={R}",
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
        // Принудительный refresh: инвалидируем кеш PV и перечитываем из БД.
        _pvCache.Invalidate();
        await ReloadFromDatabaseCoreAsync();
    }

    private async Task ReloadFromDatabaseCoreAsync()
    {
        IsBusy = true;
        try
        {
            var typesTask = _volumeTypeCache.GetAllAsync();
            var classifiersTask = _classifierCache.GetAllAsync();
            var unitsTask = _unitCache.GetAllAsync();
            var itemsTask = ShowArchived
                ? _pvService.GetAllAsync(true)
                : LoadFromCacheAsync();

            await Task.WhenAll(typesTask, classifiersTask, unitsTask, itemsTask);

            _allLoadedItems = itemsTask.Result.ToList();
            EnrichNavigations(_allLoadedItems, typesTask.Result, classifiersTask.Result, unitsTask.Result);
            ApplyFilters();
            SyncVolumeTypes(typesTask.Result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<IReadOnlyList<PhysicalVolume>> LoadFromCacheAsync()
        => await _pvCache.GetAllAsync();

    /// <summary>
    /// Подставляет ссылки на VolumeType/Classifier/Unit из кешей. GetAllAsync
    /// сейчас не делает Include, чтобы не платить за JOIN при каждом чтении.
    /// </summary>
    private static void EnrichNavigations(
        IReadOnlyList<PhysicalVolume> items,
        IReadOnlyList<PhysicalVolumeType> volumeTypes,
        IReadOnlyList<IicWorkDirection> classifiers,
        IReadOnlyList<UnitOfMeasurement> units)
    {
        var typeById = volumeTypes.ToDictionary(t => t.Id);
        var classifierById = classifiers.ToDictionary(c => c.Id);
        var unitById = units.ToDictionary(u => u.Id);

        foreach (var item in items)
        {
            item.VolumeType = typeById.TryGetValue(item.VolumeTypeId, out var type) ? type : null!;
            item.Classifier = item.ClassifierId.HasValue
                              && classifierById.TryGetValue(item.ClassifierId.Value, out var c)
                ? c
                : null;
            item.Unit = item.UnitId.HasValue
                        && unitById.TryGetValue(item.UnitId.Value, out var u)
                ? u
                : null;
            item.ClassifierNameWithUnit = item.Classifier is { } cl
                ? IicWorkDirectionDisplayUnit.FormatNameWithResolvedUnit(cl, classifierById)
                : string.Empty;
        }
    }

    private void SyncVolumeTypes(IReadOnlyList<PhysicalVolumeType> types)
    {
        VolumeTypes.Clear();
        foreach (var type in types)
        {
            VolumeTypes.Add(type);
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allLoadedItems.AsEnumerable();

        if (SelectedVolumeTypeIdFilter.HasValue)
        {
            filtered = filtered.Where(i => i.VolumeTypeId == SelectedVolumeTypeIdFilter.Value);
        }

        if (SelectedAssessmentFilter.HasValue)
        {
            filtered = filtered.Where(i => i.IsAssessment == SelectedAssessmentFilter.Value);
        }

        var filteredList = filtered.ToList();
        var preserveId = SelectedItem?.Id;

        // Bulk-replace вместо построчных Add/Remove на ObservableCollection:
        // 2497 индивидуальных CollectionChanged-событий заставляли DevExpress
        // GridControl пересчитывать layout каждый раз (~1.7s). Один Reset через
        // присваивание нового экземпляра — ~50ms.
        Items = new ObservableCollection<PhysicalVolume>(filteredList);

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

    /// <summary>
    /// Точечно обновляет одну строку после сохранения в модальном окне.
    /// Делает один SELECT по Id и in-place мутацию записи через INPC —
    /// без перестройки <see cref="ObservableCollection{T}"/> и без полного
    /// прохода <c>ApplyFilters</c>, которые на дереве с тысячами узлов
    /// заметно фризят UI.
    /// </summary>
    public async Task RefreshSingleItemAsync(int itemId)
    {
        if (itemId <= 0) return;

        var updated = await _pvService.GetByIdAsync(itemId);
        if (updated is null)
        {
            // Запись удалена — убираем из обоих хранилищ.
            _allLoadedItems.RemoveAll(i => i.Id == itemId);
            var stale = Items.FirstOrDefault(i => i.Id == itemId);
            if (stale is not null) Items.Remove(stale);
            return;
        }

        var typesTask = _volumeTypeCache.GetAllAsync();
        var classifiersTask = _classifierCache.GetAllAsync();
        var unitsTask = _unitCache.GetAllAsync();
        await Task.WhenAll(typesTask, classifiersTask, unitsTask);
        EnrichNavigations(new[] { updated }, typesTask.Result, classifiersTask.Result, unitsTask.Result);

        var passesFilter = MatchesActiveFilters(updated);

        var existingFlat = _allLoadedItems.FirstOrDefault(i => i.Id == itemId);
        var existingInItems = Items.FirstOrDefault(i => i.Id == itemId);

        if (existingFlat is null)
        {
            // Новая запись.
            _allLoadedItems.Add(updated);
            if (passesFilter) Items.Add(updated);
            return;
        }

        // Обновляем плоский кеш VM (in-memory). Замена по индексу вместо мутации,
        // т.к. _allLoadedItems не используется для биндинга — UI ссылается на Items.
        var flatIndex = _allLoadedItems.IndexOf(existingFlat);
        _allLoadedItems[flatIndex] = updated;

        if (existingInItems is null)
        {
            if (passesFilter) Items.Add(updated);
            return;
        }

        if (!passesFilter)
        {
            // Запись больше не подходит под активные фильтры.
            Items.Remove(existingInItems);
            return;
        }

        // In-place мутация: тот же объект остаётся в Items, грид перерисовывает
        // только эту строку через PropertyChanged.
        ApplyRefreshedItem(existingInItems, updated);
    }

    private bool MatchesActiveFilters(PhysicalVolume item)
    {
        if (!ShowArchived && item.IsArchived) return false;
        if (SelectedVolumeTypeIdFilter.HasValue && item.VolumeTypeId != SelectedVolumeTypeIdFilter.Value) return false;
        if (SelectedAssessmentFilter.HasValue && item.IsAssessment != SelectedAssessmentFilter.Value) return false;
        return true;
    }
}

public sealed class AssessmentFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}
