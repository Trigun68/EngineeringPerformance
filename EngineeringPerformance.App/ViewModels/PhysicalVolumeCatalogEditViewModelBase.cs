using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

/// <summary>Базовая форма редактирования номенклатуры КФО/БЕР (без выбора вида).</summary>
public abstract partial class PhysicalVolumeCatalogEditViewModelBase<T> : ObservableObject
    where T : DirectoryItem, new()
{
    private readonly IDirectoryService<T> _service;
    private readonly IDirectoryCache<T> _catalogCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int? _selectedClassifierId;
    [ObservableProperty] private int? _selectedParentId;
    [ObservableProperty] private ClassifierDisplayItem? _selectedClassifierItem;
    [ObservableProperty] private ParentDisplayItem? _selectedParentItem;

    partial void OnSelectedClassifierItemChanged(ClassifierDisplayItem? value) => SelectedClassifierId = value?.Id;
    partial void OnSelectedParentItemChanged(ParentDisplayItem? value) => SelectedParentId = value?.Id;

    [ObservableProperty] private bool? _isAssessment;
    [ObservableProperty] private AssessmentOption? _selectedAssessmentItem;

    partial void OnSelectedAssessmentItemChanged(AssessmentOption? value) => IsAssessment = value?.Value;

    [ObservableProperty] private decimal _coefficient = 1m;
    [ObservableProperty] private int? _selectedUnitId;
    [ObservableProperty] private UnitOfMeasurement? _selectedUnit;

    partial void OnSelectedUnitChanged(UnitOfMeasurement? value)
        => SelectedUnitId = value is null || value.Id == 0 ? null : value.Id;

    public ObservableCollection<AssessmentOption> AssessmentOptions { get; } =
    [
        new AssessmentOption { Value = null, Display = "Не выбрано" },
        new AssessmentOption { Value = true, Display = "Да" },
        new AssessmentOption { Value = false, Display = "Нет" },
    ];

    public ObservableCollection<IicWorkDirection> Classifiers { get; } = [];
    public ObservableCollection<UnitOfMeasurement> Units { get; } = [];
    public ObservableCollection<ClassifierDisplayItem> ClassifierDisplayItems { get; } = [];
    public ObservableCollection<ParentDisplayItem> ParentDisplayItems { get; } = [];

    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(AreLookupsReady))] private bool _isLookupsLoading;
    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty] private string _windowTitle = "Новая номенклатура";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _entityId;

    protected abstract string NewItemTitle { get; }
    protected abstract string EditItemTitle { get; }

    protected PhysicalVolumeCatalogEditViewModelBase(
        IDirectoryService<T> service,
        IDirectoryCache<T> catalogCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache)
    {
        _service = service;
        _catalogCache = catalogCache;
        _classifierCache = classifierCache;
        _unitCache = unitCache;
    }

    public void PrepareSync(T? entity, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        IsLookupsLoading = true;

        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        SelectedClassifierId = null;
        SelectedParentId = null;
        SelectedUnitId = null;
        IsAssessment = null;
        Coefficient = 1m;

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = EditItemTitle;
            EntityId = entity.Id;
            LoadFieldsFromEntity(entity);
        }
        else
        {
            IsEditMode = false;
            WindowTitle = NewItemTitle;
            EntityId = 0;
        }

        SelectedAssessmentItem = AssessmentOptions.FirstOrDefault(o => o.Value == IsAssessment) ?? AssessmentOptions[0];
    }

    protected abstract void LoadFieldsFromEntity(T entity);
    protected abstract void ApplyFieldsToEntity(T entity);

    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var excludeId = IsEditMode ? EntityId : (int?)null;
            var classifiersTask = _classifierCache.GetAllAsync(cancellationToken);
            var allVolumesTask = _catalogCache.GetAllAsync(cancellationToken);
            var unitsTask = _unitCache.GetAllAsync(cancellationToken);
            await Task.WhenAll(classifiersTask, allVolumesTask, unitsTask);

            Classifiers.Clear();
            foreach (var c in classifiersTask.Result)
                Classifiers.Add(c);

            Units.Clear();
            Units.Add(new UnitOfMeasurement { Id = 0, Name = "(не выбрано)" });
            foreach (var u in unitsTask.Result)
                Units.Add(u);

            BuildClassifierDisplayItems(classifiersTask.Result);
            BuildParentDisplayItems(BuildParentLookup(allVolumesTask.Result, excludeId));

            SelectedClassifierItem = ClassifierDisplayItems.FirstOrDefault(i => i.Id == SelectedClassifierId)
                                     ?? ClassifierDisplayItems.FirstOrDefault();
            SelectedParentItem = ParentDisplayItems.FirstOrDefault(i => i.Id == SelectedParentId)
                                 ?? ParentDisplayItems.FirstOrDefault();
            SelectedUnit = SelectedUnitId.HasValue
                ? Units.FirstOrDefault(u => u.Id == SelectedUnitId.Value) ?? Units[0]
                : Units[0];
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
        if (classifiers.Count == 0) return;

        var byId = classifiers.ToDictionary(c => c.Id);
        var childrenByParent = classifiers.OrderBy(c => c.Code).ToLookup(c => c.ParentId);
        var visited = new HashSet<int>();
        foreach (var root in childrenByParent[null])
            AppendClassifier(root, childrenByParent, byId, 0, visited);
    }

    private void AppendClassifier(
        IicWorkDirection item,
        ILookup<int?, IicWorkDirection> childrenByParent,
        IReadOnlyDictionary<int, IicWorkDirection> byId,
        int depth,
        HashSet<int> visited)
    {
        if (!visited.Add(item.Id)) return;
        var indent = new string(' ', depth * 3);
        var prefix = depth > 0 ? "└ " : string.Empty;
        ClassifierDisplayItems.Add(new ClassifierDisplayItem
        {
            Id = item.Id,
            Display = $"{indent}{prefix}{item.Code} {IicWorkDirectionDisplayUnit.FormatNameWithResolvedUnit(item, byId)}",
        });
        foreach (var child in childrenByParent[item.Id])
            AppendClassifier(child, childrenByParent, byId, depth + 1, visited);
    }

    private static IReadOnlyList<T> BuildParentLookup(IReadOnlyList<T> all, int? excludeId)
    {
        if (!excludeId.HasValue) return all;

        var childrenByParent = all.Where(i => i.ParentId.HasValue).ToLookup(i => i.ParentId!.Value, i => i.Id);
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

    private void BuildParentDisplayItems(IReadOnlyList<T> parents)
    {
        ParentDisplayItems.Clear();
        ParentDisplayItems.Add(new ParentDisplayItem { Id = null, Display = "Не выбрано" });
        if (parents.Count == 0) return;

        var childrenByParent = parents.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToLookup(p => p.ParentId);
        var visited = new HashSet<int>();
        foreach (var root in childrenByParent[null])
            AppendParent(root, childrenByParent, 0, visited);
    }

    private void AppendParent(T item, ILookup<int?, T> childrenByParent, int depth, HashSet<int> visited)
    {
        if (!visited.Add(item.Id)) return;
        var indent = new string(' ', depth * 3);
        var prefix = depth > 0 ? "└ " : string.Empty;
        var codePart = GetParentDisplayCode(item);
        ParentDisplayItems.Add(new ParentDisplayItem
        {
            Id = item.Id,
            Display = $"{indent}{prefix}{codePart}{item.Name}",
        });
        foreach (var child in childrenByParent[item.Id])
            AppendParent(child, childrenByParent, depth + 1, visited);
    }

    protected abstract string GetParentDisplayCode(T item);

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
            if (IsEditMode)
            {
                var update = new T { Id = EntityId };
                ApplyFieldsToEntity(update);
                await _service.UpdateAsync(update);
                _catalogCache.Upsert(update);
            }
            else
            {
                var entity = new T();
                ApplyFieldsToEntity(entity);
                await _service.CreateAsync(entity);
                EntityId = entity.Id;
                _catalogCache.Upsert(entity);
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
            _closeDialog(result);
        else
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                    window.DialogResult = result;
            });
    }
}

public sealed partial class PhysicalVolumeKfoEditViewModel : PhysicalVolumeCatalogEditViewModelBase<PhysicalVolumeKfo>
{
    protected override string NewItemTitle => "Новая номенклатура КФО";
    protected override string EditItemTitle => "Редактирование номенклатуры КФО";

    public PhysicalVolumeKfoEditViewModel(
        IDirectoryService<PhysicalVolumeKfo> service,
        IDirectoryCache<PhysicalVolumeKfo> catalogCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache)
        : base(service, catalogCache, classifierCache, unitCache)
    {
    }

    protected override void LoadFieldsFromEntity(PhysicalVolumeKfo entity)
    {
        Code = entity.Code;
        Name = entity.Name;
        Description = entity.Description;
        SelectedClassifierId = entity.ClassifierId;
        SelectedParentId = entity.ParentId;
        SelectedUnitId = entity.UnitId;
        IsAssessment = entity.IsAssessment;
        Coefficient = entity.Coefficient;
    }

    protected override void ApplyFieldsToEntity(PhysicalVolumeKfo entity)
    {
        entity.Code = (Code ?? string.Empty).Trim();
        entity.Description = (Description ?? string.Empty).Trim();
        entity.Name = Name.Trim();
        entity.ClassifierId = SelectedClassifierId;
        entity.ParentId = SelectedParentId;
        entity.UnitId = SelectedUnitId;
        entity.IsAssessment = IsAssessment;
        entity.Coefficient = Coefficient;
    }

    protected override string GetParentDisplayCode(PhysicalVolumeKfo item) =>
        string.IsNullOrWhiteSpace(item.Code) ? string.Empty : $"{item.Code} ";
}

public sealed partial class PhysicalVolumeBerEditViewModel : PhysicalVolumeCatalogEditViewModelBase<PhysicalVolumeBer>
{
    protected override string NewItemTitle => "Новая номенклатура БЕР";
    protected override string EditItemTitle => "Редактирование номенклатуры БЕР";

    public PhysicalVolumeBerEditViewModel(
        IDirectoryService<PhysicalVolumeBer> service,
        IDirectoryCache<PhysicalVolumeBer> catalogCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache)
        : base(service, catalogCache, classifierCache, unitCache)
    {
    }

    protected override void LoadFieldsFromEntity(PhysicalVolumeBer entity)
    {
        Code = entity.Code;
        Name = entity.Name;
        Description = entity.Description;
        SelectedClassifierId = entity.ClassifierId;
        SelectedParentId = entity.ParentId;
        SelectedUnitId = entity.UnitId;
        IsAssessment = entity.IsAssessment;
        Coefficient = entity.Coefficient;
    }

    protected override void ApplyFieldsToEntity(PhysicalVolumeBer entity)
    {
        entity.Code = (Code ?? string.Empty).Trim();
        entity.Description = (Description ?? string.Empty).Trim();
        entity.Name = Name.Trim();
        entity.ClassifierId = SelectedClassifierId;
        entity.ParentId = SelectedParentId;
        entity.UnitId = SelectedUnitId;
        entity.IsAssessment = IsAssessment;
        entity.Coefficient = Coefficient;
    }

    protected override string GetParentDisplayCode(PhysicalVolumeBer item) =>
        string.IsNullOrWhiteSpace(item.Code) ? string.Empty : $"{item.Code} ";
}
