using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IicWorkDirectionNormEditViewModel : ObservableObject
{
    private readonly IRegisterService<IicWorkDirectionNorm> _service;
    private readonly IDirectoryCache<IicWorkDirection> _directionCache;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private Action<bool>? _closeDialog;
    private int? _preselectedWorkDirectionId;

    [ObservableProperty]
    private int? _selectedWorkDirectionId;

    [ObservableProperty]
    private int? _selectedUnitId;

    [ObservableProperty]
    private ObservableCollection<IicWorkDirection> _workDirections = [];

    [ObservableProperty]
    private ObservableCollection<IicDirectionDisplayItem> _workDirectionItems = [];

    [ObservableProperty]
    private ObservableCollection<IicWorkDirectionNorm> _historyItems = [];

    [ObservableProperty]
    private IicWorkDirectionNorm? _selectedHistoryItem;

    [ObservableProperty]
    private string _inheritedUnitName = "—";

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreLookupsReady))]
    private bool _isLookupsLoading;

    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty]
    private string _windowTitle = "Новая норма группы физ. объёмов (IIC)";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public IicWorkDirectionNormEditViewModel(
        IRegisterService<IicWorkDirectionNorm> service,
        IDirectoryCache<IicWorkDirection> directionCache,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _service = service;
        _directionCache = directionCache;
        _contextFactory = contextFactory;
    }

    public void PrepareSync(
        IicWorkDirectionNorm? item,
        int? preselectedWorkDirectionId = null,
        Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        _preselectedWorkDirectionId = preselectedWorkDirectionId;
        ErrorMessage = null;
        IsLookupsLoading = true;

        // Очистка полей
        SelectedWorkDirectionId = null;
        SelectedUnitId = null;
        InheritedUnitName = "—";
        HistoryItems.Clear();
        SelectedHistoryItem = null;

        if (item is not null)
        {
            EntityId = item.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование нормы группы физ. объёмов (IIC)";
            SelectedWorkDirectionId = item.WorkDirectionId;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая норма группы физ. объёмов (IIC)";
            EntityId = 0;
            if (preselectedWorkDirectionId.HasValue)
                SelectedWorkDirectionId = preselectedWorkDirectionId.Value;
        }
    }

    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directions = await _directionCache.GetAllAsync(cancellationToken);
            WorkDirections = new ObservableCollection<IicWorkDirection>(directions);
            WorkDirectionItems = new ObservableCollection<IicDirectionDisplayItem>(FlattenDirections(directions));
            RefreshInheritedUnit();
            await LoadHistoryAsync();
        }
        finally
        {
            IsLookupsLoading = false;
        }
    }

    private static List<IicDirectionDisplayItem> FlattenDirections(IReadOnlyList<IicWorkDirection> items)
    {
        var byParent = items
            .GroupBy(i => i.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList());
        var result = new List<IicDirectionDisplayItem>();
        void Visit(int parentId, int depth)
        {
            if (!byParent.TryGetValue(parentId, out var siblings)) return;
            foreach (var s in siblings)
            {
                var indent = new string(' ', depth * 4);
                var label = string.IsNullOrWhiteSpace(s.Name) ? s.Code : $"{s.Code} — {s.Name}";
                result.Add(new IicDirectionDisplayItem { Id = s.Id, DisplayText = indent + label });
                Visit(s.Id, depth + 1);
            }
        }
        Visit(0, 0);
        return result;
    }

    private async Task LoadHistoryAsync()
    {
        if (!SelectedWorkDirectionId.HasValue || !IsEditMode)
        {
            HistoryItems.Clear();
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var history = await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.WorkDirectionId == SelectedWorkDirectionId.Value)
            .OrderByDescending(n => n.EffectiveFrom)
            .ToListAsync();

        HistoryItems = new ObservableCollection<IicWorkDirectionNorm>(history);
    }

    partial void OnSelectedWorkDirectionIdChanged(int? value)
    {
        RefreshInheritedUnit();
        _ = LoadHistoryAsync();
    }

    private void RefreshInheritedUnit()
    {
        if (WorkDirections.Count == 0)
        {
            InheritedUnitName = "—";
            SelectedUnitId = null;
            return;
        }

        var byId = WorkDirections.ToDictionary(d => d.Id);
        if (SelectedWorkDirectionId is int id && byId.TryGetValue(id, out var dir))
        {
            InheritedUnitName = IicWorkDirectionDisplayUnit.ResolveUnitName(dir, byId) ?? "—";
            SelectedUnitId = IicWorkDirectionDisplayUnit.ResolveUnitId(dir, byId);
        }
        else
        {
            InheritedUnitName = "—";
            SelectedUnitId = null;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    [RelayCommand]
    private async Task AddHistoryItemAsync()
    {
        if (!SelectedWorkDirectionId.HasValue)
        {
            ErrorMessage = "Сначала выберите группу";
            return;
        }

        var vm = new IicWorkDirectionNormHistoryEditViewModel(_service);
        var dialog = new IicWorkDirectionNormHistoryEditWindow(vm);
        vm.PrepareSync(null, GetSelectedWorkDirectionName(), InheritedUnitName, SelectedWorkDirectionId.Value, SelectedUnitId);

        if (dialog.ShowDialog() == true)
        {
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task EditHistoryItemAsync()
    {
        if (SelectedHistoryItem is null) return;

        var vm = new IicWorkDirectionNormHistoryEditViewModel(_service);
        var dialog = new IicWorkDirectionNormHistoryEditWindow(vm);
        vm.PrepareSync(SelectedHistoryItem, GetSelectedWorkDirectionName(), InheritedUnitName, SelectedWorkDirectionId!.Value, SelectedUnitId);

        if (dialog.ShowDialog() == true)
        {
            await LoadHistoryAsync();
        }
    }

    private string GetSelectedWorkDirectionName()
    {
        if (SelectedWorkDirectionId.HasValue && WorkDirections.FirstOrDefault(w => w.Id == SelectedWorkDirectionId.Value) is var dir && dir is not null)
        {
            return dir.Name;
        }
        return "—";
    }

    [RelayCommand]
    private async Task DeleteHistoryItemAsync()
    {
        if (SelectedHistoryItem is null) return;

        var result = MessageBox.Show(
            "Удалить выбранную запись из истории?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _service.DeleteAsync(SelectedHistoryItem.Id);
            await LoadHistoryAsync();
        }
    }

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
        {
            _closeDialog(result);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                {
                    window.DialogResult = result;
                }
            });
        }
    }
}

public sealed class IicDirectionDisplayItem
{
    public int Id { get; init; }
    public string DisplayText { get; init; } = string.Empty;
}
