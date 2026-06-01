using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class EquipmentEditViewModel : ObservableObject
{
    private readonly IDirectoryService<Equipment> _service;
    private readonly IDirectoryCache<Equipment> _equipmentCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int? _selectedClassifierId;

    [ObservableProperty]
    private int? _selectedParentId;

    [ObservableProperty]
    private ClassifierDisplayItem? _selectedClassifierItem;

    [ObservableProperty]
    private EquipmentParentDisplayItem? _selectedParentItem;

    partial void OnSelectedClassifierItemChanged(ClassifierDisplayItem? value)
    {
        SelectedClassifierId = value?.Id;
    }

    partial void OnSelectedParentItemChanged(EquipmentParentDisplayItem? value)
    {
        SelectedParentId = value?.Id;
    }

    [ObservableProperty]
    private bool? _isAssessment;

    [ObservableProperty]
    private AssessmentOption? _selectedAssessmentItem;

    partial void OnSelectedAssessmentItemChanged(AssessmentOption? value)
    {
        IsAssessment = value?.Value;
    }

    [ObservableProperty]
    private decimal? _coefficient;

    public ObservableCollection<AssessmentOption> AssessmentOptions { get; } =
    [
        new AssessmentOption { Value = null, Display = "Не выбрано" },
        new AssessmentOption { Value = true, Display = "Да" },
        new AssessmentOption { Value = false, Display = "Нет" },
    ];

    public ObservableCollection<ClassifierDisplayItem> ClassifierDisplayItems { get; } = [];

    public ObservableCollection<EquipmentParentDisplayItem> ParentDisplayItems { get; } = [];

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreLookupsReady))]
    private bool _isLookupsLoading;

    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty]
    private string _windowTitle = "Новая номенклатура оборудования";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public EquipmentEditViewModel(
        IDirectoryService<Equipment> service,
        IDirectoryCache<Equipment> equipmentCache,
        IDirectoryCache<IicWorkDirection> classifierCache)
    {
        _service = service;
        _equipmentCache = equipmentCache;
        _classifierCache = classifierCache;
    }

    public void PrepareSync(Equipment? entity, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        IsLookupsLoading = true;

        Name = string.Empty;
        Description = string.Empty;
        SelectedClassifierId = null;
        SelectedParentId = null;
        IsAssessment = null;
        Coefficient = null;

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование номенклатуры оборудования";
            EntityId = entity.Id;
            Name = entity.Name;
            Description = entity.Description;
            SelectedClassifierId = entity.ClassifierId;
            SelectedParentId = entity.ParentId;
            IsAssessment = entity.IsAssessment;
            Coefficient = entity.Coefficient;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая номенклатура оборудования";
            EntityId = 0;
        }

        SelectedAssessmentItem = AssessmentOptions.FirstOrDefault(o => o.Value == IsAssessment)
                                 ?? AssessmentOptions[0];
    }

    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var excludeId = IsEditMode ? EntityId : (int?)null;

            var classifiersTask = _classifierCache.GetAllAsync(cancellationToken);
            var allEquipmentTask = _equipmentCache.GetAllAsync(cancellationToken);

            await Task.WhenAll(classifiersTask, allEquipmentTask);

            var classifiers = classifiersTask.Result;
            var parents = BuildParentLookup(allEquipmentTask.Result, excludeId);

            BuildClassifierDisplayItems(classifiers);
            BuildParentDisplayItems(parents);

            SelectedClassifierItem = ClassifierDisplayItems.FirstOrDefault(i => i.Id == SelectedClassifierId)
                                     ?? ClassifierDisplayItems.FirstOrDefault();
            SelectedParentItem = ParentDisplayItems.FirstOrDefault(i => i.Id == SelectedParentId)
                                 ?? ParentDisplayItems.FirstOrDefault();
        }
        finally
        {
            IsLookupsLoading = false;
        }
    }

    private void BuildClassifierDisplayItems(IReadOnlyList<IicWorkDirection> classifiers)
    {
        ClassifierDisplayItems.Clear();
        ClassifierDisplayItems.Add(new ClassifierDisplayItem { Id = null, Display = "Не выбрано" });

        if (classifiers.Count == 0)
            return;

        var byId = classifiers.ToDictionary(c => c.Id);
        var childrenByParent = classifiers
            .OrderBy(c => c.Code)
            .ToLookup(c => c.ParentId);

        var visited = new HashSet<int>();
        foreach (var root in childrenByParent[null])
        {
            AppendClassifier(root, childrenByParent, byId, depth: 0, visited);
        }
    }

    private void AppendClassifier(
        IicWorkDirection item,
        ILookup<int?, IicWorkDirection> childrenByParent,
        IReadOnlyDictionary<int, IicWorkDirection> byId,
        int depth,
        HashSet<int> visited)
    {
        if (!visited.Add(item.Id))
            return;

        var indent = new string(' ', depth * 3);
        var prefix = depth > 0 ? "└ " : string.Empty;
        var nameWithUnit = IicWorkDirectionDisplayUnit.FormatNameWithResolvedUnit(item, byId);
        ClassifierDisplayItems.Add(new ClassifierDisplayItem
        {
            Id = item.Id,
            Display = $"{indent}{prefix}{item.Code} {nameWithUnit}",
        });

        foreach (var child in childrenByParent[item.Id])
        {
            AppendClassifier(child, childrenByParent, byId, depth + 1, visited);
        }
    }

    private static IReadOnlyList<Equipment> BuildParentLookup(
        IReadOnlyList<Equipment> all,
        int? excludeId)
    {
        if (!excludeId.HasValue)
            return all;

        var childrenByParent = all
            .Where(i => i.ParentId.HasValue)
            .ToLookup(i => i.ParentId!.Value, i => i.Id);

        var excluded = new HashSet<int> { excludeId.Value };
        var stack = new Stack<int>();
        stack.Push(excludeId.Value);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var childId in childrenByParent[current])
            {
                if (excluded.Add(childId))
                    stack.Push(childId);
            }
        }

        return all.Where(i => !excluded.Contains(i.Id)).ToList();
    }

    private void BuildParentDisplayItems(IReadOnlyList<Equipment> parents)
    {
        ParentDisplayItems.Clear();
        ParentDisplayItems.Add(new EquipmentParentDisplayItem { Id = null, Display = "Не выбрано" });

        if (parents.Count == 0)
            return;

        var childrenByParent = parents
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToLookup(p => p.ParentId);

        var visited = new HashSet<int>();
        foreach (var root in childrenByParent[null])
        {
            AppendParent(root, childrenByParent, depth: 0, visited);
        }
    }

    private void AppendParent(
        Equipment item,
        ILookup<int?, Equipment> childrenByParent,
        int depth,
        HashSet<int> visited)
    {
        if (!visited.Add(item.Id))
            return;

        var indent = new string(' ', depth * 3);
        var prefix = depth > 0 ? "└ " : string.Empty;
        ParentDisplayItems.Add(new EquipmentParentDisplayItem
        {
            Id = item.Id,
            Display = $"{indent}{prefix}{item.Name}",
        });

        foreach (var child in childrenByParent[item.Id])
        {
            AppendParent(child, childrenByParent, depth + 1, visited);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Наименование обязательно";
            return;
        }

        try
        {
            var nameValue = Name.Trim();
            var descriptionValue = (Description ?? string.Empty).Trim();

            if (IsEditMode)
            {
                var update = new Equipment
                {
                    Id = EntityId,
                    Name = nameValue,
                    Description = descriptionValue,
                    ClassifierId = SelectedClassifierId,
                    ParentId = SelectedParentId,
                    IsAssessment = IsAssessment,
                    Coefficient = Coefficient,
                };

                await _service.UpdateAsync(update);
                _equipmentCache.Upsert(update);
            }
            else
            {
                var entity = new Equipment
                {
                    Name = nameValue,
                    Description = descriptionValue,
                    ClassifierId = SelectedClassifierId,
                    ParentId = SelectedParentId,
                    IsAssessment = IsAssessment,
                    Coefficient = Coefficient,
                };

                await _service.CreateAsync(entity);
                EntityId = entity.Id;
                _equipmentCache.Upsert(entity);
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

public sealed class EquipmentParentDisplayItem
{
    public int? Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
