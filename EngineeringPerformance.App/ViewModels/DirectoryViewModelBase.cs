using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using Serilog;

namespace EngineeringPerformance.App.ViewModels;

public abstract partial class DirectoryViewModelBase<T> : ObservableObject, ILoadableViewModel
    where T : DirectoryItem, new()
{
    private readonly IDirectoryService<T> _service;
    private CancellationTokenSource? _loadCts;

    public Window? OwnerWindow { get; set; }

    public UserSettingsHelper? SettingsHelper { get; set; }

    private string SettingsKey => $"ShowArchived.{GetType().Name}";

    [ObservableProperty]
    private ObservableCollection<T> _items = [];

    [ObservableProperty]
    private T? _selectedItem;

    [ObservableProperty]
    private bool _showArchived;

    private bool _restoringSettings;

    partial void OnShowArchivedChanged(bool value)
    {
        if (!_restoringSettings && SettingsHelper is not null)
            _ = SettingsHelper.SaveBoolAsync(SettingsKey, value);
        _ = LoadSafeAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchText = string.Empty;

    protected IDirectoryService<T> DirectoryService => _service;

    protected DirectoryViewModelBase(IDirectoryService<T> service)
    {
        _service = service;
    }

    /// <summary>
    /// Сбрасывает состояние VM. Singleton-VM накапливают коллекции, выделение, фильтры и
    /// удерживают ссылку на закрытое окно — после logout/смены БД это утечка состояния.
    /// Вызывается из MainWindow.Closed и при logout.
    /// </summary>
    public virtual void Reset()
    {
        try { _loadCts?.Cancel(); } catch { /* ignore */ }
        _loadCts = null;
        Items = [];
        SelectedItem = null;
        SearchText = string.Empty;
        ShowArchived = false;
        IsBusy = false;
        OwnerWindow = null;
    }

    /// <summary>Копирует поля справочника после перезагрузки из БД (переопределяется для сущностей с доп. колонками).</summary>
    protected virtual void ApplyRefreshedItem(T existing, T updated)
    {
        existing.Name = updated.Name;
        existing.ParentId = updated.ParentId;
        existing.SortOrder = updated.SortOrder;
        existing.IsArchived = updated.IsArchived;
        existing.CreatedAt = updated.CreatedAt;
        existing.ModifiedAt = updated.ModifiedAt;
    }

    /// <summary>Обновляет строки без <see cref="ObservableCollection{T}.Clear"/> — дерево и таблицы сохраняют разворот и фокус. Заменяет существующий экземпляр через индексатор, чтобы DevExpress GridControl/TreeListView гарантированно перерисовали изменённые ячейки (сущности справочников не реализуют INotifyPropertyChanged).</summary>
    protected void MergeItemsPreservingReferences(IReadOnlyList<T> fresh)
    {
        if (fresh.Count == 0)
        {
            Items.Clear();
            return;
        }

        var freshById = fresh.ToDictionary(x => x.Id);
        var preserveId = SelectedItem?.Id;

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            var row = Items[i];
            if (!freshById.TryGetValue(row.Id, out var updated))
            {
                Items.RemoveAt(i);
                continue;
            }

            Items[i] = updated;
        }

        var existingIds = Items.Select(x => x.Id).ToHashSet();
        foreach (var item in fresh)
        {
            if (!existingIds.Contains(item.Id))
            {
                Items.Add(item);
            }
        }

        if (preserveId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(i => i.Id == preserveId.Value);
        }
    }

    private async Task LoadSafeAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAsync error: {ex.Message}");
        }
    }

    public virtual async Task LoadAsync()
    {
        if (SettingsHelper is not null)
        {
            var saved = await SettingsHelper.GetBoolAsync(SettingsKey);
            if (saved != ShowArchived)
            {
                _restoringSettings = true;
                ShowArchived = saved;
                _restoringSettings = false;
                return;
            }
        }
        await LoadInternalAsync();
    }

    /// <summary>
    /// Источник данных для грида справочника. По умолчанию — прямой запрос
    /// в БД. VM, у которых уже есть прогретый <see cref="IDirectoryCache{T}"/>
    /// (типы, единицы, IIC), переопределяют для отдачи из кеша — это снимает
    /// первый «холодный» запрос на ~800 мс (EF query compilation).
    /// </summary>
    protected virtual Task<IReadOnlyList<T>> FetchAllAsync()
        => _service.GetAllAsync(ShowArchived);

    public virtual async Task ReloadFromDatabaseAsync()
    {
        IsBusy = true;
        try
        {
            var items = await FetchAllAsync();
            MergeItemsPreservingReferences(items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadInternalAsync()
    {
        if (OwnerWindow is not null)
        {
            var typeName = typeof(T).Name switch
            {
                "User" => "Пользователи",
                "Project" => "Проекты",
                "PhysicalVolume" => "Номенклатура физических объёмов",
                "PhysicalVolumeType" => "Виды справочников физ. объёмов",
                "IicWorkDirection" => "Группы физ. объёмов (IIC)",
                "LaborIntensity" => "Трудоёмкость",
                "UnitOfMeasurement" => "Единицы измерения",
                "ProjectAnalysisVersion" => "Версии анализа проекта",
                _ => "Справочник"
            };

            await LoadingWindow.ShowAsync(OwnerWindow, async vm =>
            {
                var swTotal = Stopwatch.StartNew();
                vm.SetTitle($"Загрузка: {typeName}");
                vm.SetProgress(0, "Подключение к базе данных...");

                IsBusy = true;
                vm.SetProgress(30, "Загрузка данных...");

                var swFetch = Stopwatch.StartNew();
                var items = await FetchAllAsync();
                swFetch.Stop();

                vm.SetProgress(80, "Обработка данных...");
                var swMerge = Stopwatch.StartNew();
                MergeItemsPreservingReferences(items);
                swMerge.Stop();

                vm.SetProgress(100, "Готово");
                IsBusy = false;
                swTotal.Stop();

                Log.Information("[PERF] DirectoryVM<{Type}>.LoadInternal fetch={F}ms merge={M}ms total={T}ms rows={R}",
                    typeof(T).Name, swFetch.ElapsedMilliseconds, swMerge.ElapsedMilliseconds,
                    swTotal.ElapsedMilliseconds, items.Count);
            });
        }
        else
        {
            var swTotal = Stopwatch.StartNew();
            IsBusy = true;
            try
            {
                var swFetch = Stopwatch.StartNew();
                var items = await FetchAllAsync();
                swFetch.Stop();
                var swMerge = Stopwatch.StartNew();
                MergeItemsPreservingReferences(items);
                swMerge.Stop();
                Log.Information("[PERF] DirectoryVM<{Type}>.LoadInternal(noOwner) fetch={F}ms merge={M}ms total={T}ms rows={R}",
                    typeof(T).Name, swFetch.ElapsedMilliseconds, swMerge.ElapsedMilliseconds,
                    swTotal.ElapsedMilliseconds, items.Count);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadSafeAsync();

    [RelayCommand]
    private async Task DeleteItemAsync()
    {
        if (SelectedItem is null) return;

        await _service.DeleteAsync(SelectedItem.Id);
        Items.Remove(SelectedItem);
        SelectedItem = null;
    }

    [RelayCommand]
    private async Task ArchiveItemAsync()
    {
        if (SelectedItem is null) return;

        await _service.ArchiveAsync(SelectedItem.Id);
        if (!ShowArchived)
        {
            Items.Remove(SelectedItem);
        }
        SelectedItem = null;
    }

    [RelayCommand]
    private async Task UnarchiveItemAsync()
    {
        if (SelectedItem is null) return;

        await _service.UnarchiveAsync(SelectedItem.Id);
        SelectedItem.IsArchived = false;
        SelectedItem = null;
    }

    [RelayCommand]
    private async Task ToggleArchivedAsync()
    {
        ShowArchived = !ShowArchived;
        await LoadSafeAsync();
    }

    public async Task HandleDragDropAsync(int draggedItemId, int? targetParentId, int targetSortOrder)
    {
        await _service.UpdateSortOrderAsync(draggedItemId, targetParentId, targetSortOrder);

        var item = Items.FirstOrDefault(i => i.Id == draggedItemId);
        if (item is not null)
        {
            item.ParentId = targetParentId;
            item.SortOrder = targetSortOrder;
        }
    }

    protected virtual void RefreshItemInCollection(T item)
    {
        var idx = -1;
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == item.Id) { idx = i; break; }
        }
        if (idx < 0) return;

        var wasSelected = ReferenceEquals(SelectedItem, Items[idx]);
        Items[idx] = item;
        if (wasSelected) SelectedItem = item;
    }

    public async Task RefreshItemFromServiceAsync(int itemId)
    {
        if (itemId <= 0)
            return;

        var updated = await _service.GetByIdAsync(itemId);
        if (updated is not null)
        {
            RefreshItemInCollection(updated);
        }
    }
}
