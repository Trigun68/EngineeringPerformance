using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed class IicTreeNode
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

public sealed class LaborIntensityTreeNode
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

public sealed partial class LaborIntensityEditViewModel : ObservableObject
{
    private readonly IDirectoryService<LaborIntensity> _service;
    private readonly IDirectoryCache<IicWorkDirection> _iicCache;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal? _normPpr;

    [ObservableProperty]
    private decimal? _normOpr;

    [ObservableProperty]
    private decimal? _normPd;

    [ObservableProperty]
    private decimal? _normRd;

    [ObservableProperty]
    private int? _selectedIicWorkDirectionId;

    [ObservableProperty]
    private int? _selectedParentId;

    [ObservableProperty]
    private ObservableCollection<IicTreeNode> _iicWorkDirections = [];

    [ObservableProperty]
    private ObservableCollection<LaborIntensityTreeNode> _parentItems = [];

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _windowTitle = "Новая запись трудоёмкости";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public LaborIntensityEditViewModel(
        IDirectoryService<LaborIntensity> service,
        IDirectoryCache<IicWorkDirection> iicCache)
    {
        _service = service;
        _iicCache = iicCache;
    }

    public async Task InitializeAsync(LaborIntensity? entity, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;

        var iicItems = await _iicCache.GetAllAsync();
        var iicNodes = new List<IicTreeNode> { new() { Id = 0, Depth = 0 } };
        iicNodes.AddRange(FlattenIic(iicItems));
        IicWorkDirections = new ObservableCollection<IicTreeNode>(iicNodes);

        var allLabors = await _service.GetAllAsync(includeArchived: false);
        var excludeId = entity?.Id;
        var descendants = excludeId.HasValue
            ? CollectDescendantIds(allLabors, excludeId.Value)
            : new HashSet<int>();
        var allowed = allLabors
            .Where(l => l.Id != excludeId && !descendants.Contains(l.Id))
            .ToList();
        var laborNodes = new List<LaborIntensityTreeNode> { new() { Id = 0, Depth = 0 } };
        laborNodes.AddRange(FlattenLabor(allowed));
        ParentItems = new ObservableCollection<LaborIntensityTreeNode>(laborNodes);

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование записи трудоёмкости";
            EntityId = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            NormPpr = entity.NormPpr;
            NormOpr = entity.NormOpr;
            NormPd = entity.NormPd;
            NormRd = entity.NormRd;
            SelectedIicWorkDirectionId = entity.IicWorkDirectionId ?? 0;
            SelectedParentId = entity.ParentId ?? 0;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая запись трудоёмкости";
            EntityId = 0;
            SelectedIicWorkDirectionId = 0;
            SelectedParentId = 0;
        }
    }

    private static HashSet<int> CollectDescendantIds(IReadOnlyList<LaborIntensity> all, int rootId)
    {
        var byParent = all.GroupBy(l => l.ParentId).ToDictionary(g => g.Key ?? 0, g => g.ToList());
        var result = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!byParent.TryGetValue(current, out var children)) continue;
            foreach (var child in children)
            {
                if (result.Add(child.Id)) queue.Enqueue(child.Id);
            }
        }
        return result;
    }

    private static List<IicTreeNode> FlattenIic(IReadOnlyList<IicWorkDirection> items)
    {
        var byParent = items
            .GroupBy(i => i.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList());
        var result = new List<IicTreeNode>();
        void Visit(int parentId, int depth)
        {
            if (!byParent.TryGetValue(parentId, out var siblings)) return;
            foreach (var s in siblings)
            {
                result.Add(new IicTreeNode { Id = s.Id, ParentId = s.ParentId, Code = s.Code, Name = s.Name, Depth = depth });
                Visit(s.Id, depth + 1);
            }
        }
        Visit(0, 0);
        return result;
    }

    private static List<LaborIntensityTreeNode> FlattenLabor(IReadOnlyList<LaborIntensity> items)
    {
        var byParent = items
            .GroupBy(i => i.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList());
        var result = new List<LaborIntensityTreeNode>();
        void Visit(int parentId, int depth)
        {
            if (!byParent.TryGetValue(parentId, out var siblings)) return;
            foreach (var s in siblings)
            {
                result.Add(new LaborIntensityTreeNode { Id = s.Id, ParentId = s.ParentId, Code = s.Code, Name = s.Name, Depth = depth });
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
                existing.NormPpr = NormPpr;
                existing.NormOpr = NormOpr;
                existing.NormPd = NormPd;
                existing.NormRd = NormRd;
                existing.IicWorkDirectionId = SelectedIicWorkDirectionId is null or 0 ? null : SelectedIicWorkDirectionId;
                existing.ParentId = SelectedParentId is null or 0 ? null : SelectedParentId;
                existing.ModifiedAt = DateTime.UtcNow;
                await _service.UpdateAsync(existing);
            }
            else
            {
                var item = new LaborIntensity
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    NormPpr = NormPpr,
                    NormOpr = NormOpr,
                    NormPd = NormPd,
                    NormRd = NormRd,
                    IicWorkDirectionId = SelectedIicWorkDirectionId is null or 0 ? null : SelectedIicWorkDirectionId,
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

