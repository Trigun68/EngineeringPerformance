using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed class IicParentNode
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Depth { get; init; }

    public string DisplayText
    {
        get
        {
            if (Id == 0) return "(не выбрано)";
            var indent = new string(' ', Depth * 4);
            var label = string.IsNullOrWhiteSpace(Name) ? Code : $"{Code} — {Name}";
            return indent + label;
        }
    }
}

public sealed class UnitDisplayItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayText => Id == 0 ? "(не выбрано)" : Name;
}

public sealed partial class IicWorkDirectionEditViewModel : ObservableObject
{
    private readonly IDirectoryService<IicWorkDirection> _service;
    private readonly IIicWorkDirectionLookupService _lookupService;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int? _selectedUnitId;

    [ObservableProperty]
    private int? _selectedParentId;

    [ObservableProperty]
    private ObservableCollection<UnitDisplayItem> _units = [];

    [ObservableProperty]
    private ObservableCollection<IicParentNode> _parents = [];

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreLookupsReady))]
    private bool _isLookupsLoading;

    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty]
    private string _windowTitle = "Новая группа физ. объёмов (IIC)";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public IicWorkDirectionEditViewModel(
        IDirectoryService<IicWorkDirection> service,
        IIicWorkDirectionLookupService lookupService,
        IDirectoryCache<UnitOfMeasurement> unitCache)
    {
        _service = service;
        _lookupService = lookupService;
        _unitCache = unitCache;
    }

    /// <summary>
    /// Мгновенно заполняет поля формы значениями из сущности (или значениями
    /// по умолчанию). Справочники подгружаются <see cref="LoadLookupsAsync"/>.
    /// </summary>
    public void PrepareSync(IicWorkDirection? item, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        IsLookupsLoading = true;

        if (item is not null)
        {
            EntityId = item.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование группы физ. объёмов (IIC)";
            Code = item.Code;
            Name = item.Name;
            Description = item.Description;
            SelectedUnitId = item.UnitId ?? 0;
            SelectedParentId = item.ParentId ?? 0;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая группа физ. объёмов (IIC)";
            EntityId = 0;
            SelectedUnitId = 0;
            SelectedParentId = 0;
        }
    }

    /// <summary>
    /// Параллельно загружает единицы измерения (из кеша) и список возможных
    /// родителей (лёгкая проекция без JOIN и без самого элемента/потомков).
    /// </summary>
    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var excludeId = IsEditMode ? EntityId : (int?)null;

            var unitsTask = _unitCache.GetAllAsync(cancellationToken);
            var parentsTask = _lookupService.GetLookupAsync(
                includeArchived: false,
                excludeId: excludeId,
                cancellationToken: cancellationToken);

            await Task.WhenAll(unitsTask, parentsTask);

            var unitItems = new List<UnitDisplayItem> { new() { Id = 0 } };
            unitItems.AddRange(unitsTask.Result.Select(u => new UnitDisplayItem { Id = u.Id, Name = u.Name }));
            Units = new ObservableCollection<UnitDisplayItem>(unitItems);

            var parentNodes = new List<IicParentNode> { new() { Id = 0, Depth = 0 } };
            parentNodes.AddRange(FlattenIic(parentsTask.Result));
            Parents = new ObservableCollection<IicParentNode>(parentNodes);
        }
        finally
        {
            IsLookupsLoading = false;
        }
    }

    private static List<IicParentNode> FlattenIic(IReadOnlyList<IicWorkDirectionLookupItem> items)
    {
        var byParent = items
            .GroupBy(i => i.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Code).ThenBy(x => x.Name).ToList());
        var result = new List<IicParentNode>();
        void Visit(int parentId, int depth)
        {
            if (!byParent.TryGetValue(parentId, out var siblings)) return;
            foreach (var s in siblings)
            {
                result.Add(new IicParentNode { Id = s.Id, ParentId = s.ParentId, Code = s.Code, Name = s.Name, Depth = depth });
                Visit(s.Id, depth + 1);
            }
        }
        Visit(0, 0);
        return result;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Код и наименование обязательны";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                var existing = await _service.GetByIdAsync(EntityId);
                if (existing is null)
                {
                    ErrorMessage = "Запись не найдена";
                    return;
                }

                existing.Code = Code.Trim();
                existing.Name = Name.Trim();
                existing.Description = Description.Trim();
                existing.UnitId = SelectedUnitId is null or 0 ? null : SelectedUnitId;
                existing.ParentId = SelectedParentId is null or 0 ? null : SelectedParentId;
                existing.ModifiedAt = DateTime.UtcNow;
                await _service.UpdateAsync(existing);
            }
            else
            {
                var item = new IicWorkDirection
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    Description = Description.Trim(),
                    UnitId = SelectedUnitId is null or 0 ? null : SelectedUnitId,
                    ParentId = SelectedParentId is null or 0 ? null : SelectedParentId,
                };
                await _service.CreateAsync(item);
            }

            CloseDialog(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

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
