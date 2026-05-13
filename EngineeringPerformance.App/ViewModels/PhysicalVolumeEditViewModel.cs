using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PhysicalVolumeEditViewModel : ObservableObject
{
    private readonly IDirectoryService<PhysicalVolume> _service;
    private readonly IDirectoryCache<PhysicalVolume> _physicalVolumeCache;
    private readonly IDirectoryCache<PhysicalVolumeType> _volumeTypeCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private readonly IDirectoryCache<UnitOfMeasurement> _unitCache;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _selectedVolumeTypeId;

    [ObservableProperty]
    private int? _selectedClassifierId;

    [ObservableProperty]
    private int? _selectedParentId;

    [ObservableProperty]
    private ClassifierDisplayItem? _selectedClassifierItem;

    [ObservableProperty]
    private ParentDisplayItem? _selectedParentItem;

    partial void OnSelectedClassifierItemChanged(ClassifierDisplayItem? value)
    {
        SelectedClassifierId = value?.Id;
    }

    partial void OnSelectedParentItemChanged(ParentDisplayItem? value)
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
    private decimal _coefficient = 1m;

    [ObservableProperty]
    private int? _selectedUnitId;

    [ObservableProperty]
    private UnitOfMeasurement? _selectedUnit;

    partial void OnSelectedUnitChanged(UnitOfMeasurement? value)
    {
        SelectedUnitId = value is null || value.Id == 0 ? null : value.Id;
    }

    public ObservableCollection<AssessmentOption> AssessmentOptions { get; } =
    [
        new AssessmentOption { Value = null, Display = "Не выбрано" },
        new AssessmentOption { Value = true, Display = "Да" },
        new AssessmentOption { Value = false, Display = "Нет" },
    ];

    [ObservableProperty]
    private ObservableCollection<PhysicalVolumeType> _volumeTypes = [];

    [ObservableProperty]
    private ObservableCollection<IicWorkDirection> _classifiers = [];

    [ObservableProperty]
    private ObservableCollection<UnitOfMeasurement> _units = [];

    public ObservableCollection<ClassifierDisplayItem> ClassifierDisplayItems { get; } = [];

    public ObservableCollection<ParentDisplayItem> ParentDisplayItems { get; } = [];

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreLookupsReady))]
    private bool _isLookupsLoading;

    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty]
    private string _windowTitle = "Новая номенклатура физ. объёмов";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public PhysicalVolumeEditViewModel(
        IDirectoryService<PhysicalVolume> service,
        IDirectoryCache<PhysicalVolume> physicalVolumeCache,
        IDirectoryCache<PhysicalVolumeType> volumeTypeCache,
        IDirectoryCache<IicWorkDirection> classifierCache,
        IDirectoryCache<UnitOfMeasurement> unitCache)
    {
        _service = service;
        _physicalVolumeCache = physicalVolumeCache;
        _volumeTypeCache = volumeTypeCache;
        _classifierCache = classifierCache;
        _unitCache = unitCache;
    }

    /// <summary>
    /// Мгновенно заполняет поля формы значениями из сущности (или значениями
    /// по умолчанию для новой записи). Справочники не загружаются — их тянет
    /// <see cref="LoadLookupsAsync"/> в фоне уже после показа окна.
    /// </summary>
    public void PrepareSync(PhysicalVolume? entity, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        IsLookupsLoading = true;

        // Очистка полей
        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        SelectedVolumeTypeId = 0;
        SelectedClassifierId = null;
        SelectedParentId = null;
        SelectedUnitId = null;
        IsAssessment = null;
        Coefficient = 1m;

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование номенклатуры физ. объёмов";
            EntityId = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            Description = entity.Description;
            SelectedVolumeTypeId = entity.VolumeTypeId;
            SelectedClassifierId = entity.ClassifierId;
            SelectedParentId = entity.ParentId;
            SelectedUnitId = entity.UnitId;
            IsAssessment = entity.IsAssessment;
            Coefficient = entity.Coefficient;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая номенклатура физ. объёмов";
            EntityId = 0;
        }

        SelectedAssessmentItem = AssessmentOptions.FirstOrDefault(o => o.Value == IsAssessment)
                                 ?? AssessmentOptions[0];
    }

    /// <summary>
    /// Загружает все три справочника (типы, классификаторы, родители) параллельно.
    /// Все три источника берутся из кешей: сетевых вызовов нет, если кеши прогреты.
    /// Список родителей строится из кеша PhysicalVolume с локальным исключением
    /// самого редактируемого элемента и его потомков (предотвращает цикл иерархии).
    /// </summary>
    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var excludeId = IsEditMode ? EntityId : (int?)null;

            var volumeTypesTask = _volumeTypeCache.GetAllAsync(cancellationToken);
            var classifiersTask = _classifierCache.GetAllAsync(cancellationToken);
            var allVolumesTask = _physicalVolumeCache.GetAllAsync(cancellationToken);
            var unitsTask = _unitCache.GetAllAsync(cancellationToken);

            await Task.WhenAll(volumeTypesTask, classifiersTask, allVolumesTask, unitsTask);

            var volumeTypes = volumeTypesTask.Result;
            var classifiers = classifiersTask.Result;
            var parents = BuildParentLookup(allVolumesTask.Result, excludeId);
            var unitsList = unitsTask.Result;

            VolumeTypes = new ObservableCollection<PhysicalVolumeType>(volumeTypes);
            Classifiers = new ObservableCollection<IicWorkDirection>(classifiers);

            var unitsWithSentinel = new List<UnitOfMeasurement>
            {
                new() { Id = 0, Name = "(не выбрано)" },
            };
            unitsWithSentinel.AddRange(unitsList);
            Units = new ObservableCollection<UnitOfMeasurement>(unitsWithSentinel);

            BuildClassifierDisplayItems(classifiers);
            BuildParentDisplayItems(parents);

            SelectedClassifierItem = ClassifierDisplayItems.FirstOrDefault(i => i.Id == SelectedClassifierId)
                                     ?? ClassifierDisplayItems.FirstOrDefault();
            SelectedParentItem = ParentDisplayItems.FirstOrDefault(i => i.Id == SelectedParentId)
                                 ?? ParentDisplayItems.FirstOrDefault();
            SelectedUnit = SelectedUnitId.HasValue
                ? Units.FirstOrDefault(u => u.Id == SelectedUnitId.Value) ?? Units[0]
                : Units[0];

            if (!IsEditMode && SelectedVolumeTypeId == 0 && VolumeTypes.Count > 0)
                SelectedVolumeTypeId = VolumeTypes[0].Id;
        }
        finally
        {
            IsLookupsLoading = false;
        }
    }

    /// <summary>
    /// Строит плоский список классификаторов с отступами для <see cref="ClassifierDisplayItems"/>.
    /// Первый элемент — «Не выбрано» (Id = null).
    /// Обход — O(N): используем lookup по ParentId вместо повторного Linq-скана.
    /// </summary>
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

    /// <summary>
    /// Возвращает кандидатов в родители: все активные элементы, кроме самого
    /// редактируемого и его потомков (это исключило бы цикл иерархии).
    /// </summary>
    private static IReadOnlyList<PhysicalVolume> BuildParentLookup(
        IReadOnlyList<PhysicalVolume> all,
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

    /// <summary>
    /// Строит плоский иерархический список родителей с отступами.
    /// Первый элемент — «Не выбрано» (Id = null).
    /// </summary>
    private void BuildParentDisplayItems(IReadOnlyList<PhysicalVolume> parents)
    {
        ParentDisplayItems.Clear();
        ParentDisplayItems.Add(new ParentDisplayItem { Id = null, Display = "Не выбрано" });

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
        PhysicalVolume item,
        ILookup<int?, PhysicalVolume> childrenByParent,
        int depth,
        HashSet<int> visited)
    {
        if (!visited.Add(item.Id))
            return;

        var indent = new string(' ', depth * 3);
        var prefix = depth > 0 ? "└ " : string.Empty;
        var codePart = string.IsNullOrWhiteSpace(item.Code) ? string.Empty : $"{item.Code} ";
        ParentDisplayItems.Add(new ParentDisplayItem
        {
            Id = item.Id,
            Display = $"{indent}{prefix}{codePart}{item.Name}",
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
            var codeValue = (Code ?? string.Empty).Trim();
            var nameValue = Name.Trim();
            var descriptionValue = (Description ?? string.Empty).Trim();

            if (IsEditMode)
            {
                var update = new PhysicalVolume
                {
                    Id = EntityId,
                    Code = codeValue,
                    Name = nameValue,
                    Description = descriptionValue,
                    VolumeTypeId = SelectedVolumeTypeId,
                    ClassifierId = SelectedClassifierId,
                    ParentId = SelectedParentId,
                    UnitId = SelectedUnitId,
                    IsAssessment = IsAssessment,
                    Coefficient = Coefficient,
                };

                await _service.UpdateAsync(update);
                _physicalVolumeCache.Upsert(update);
            }
            else
            {
                var entity = new PhysicalVolume
                {
                    Code = codeValue,
                    Name = nameValue,
                    Description = descriptionValue,
                    VolumeTypeId = SelectedVolumeTypeId,
                    ClassifierId = SelectedClassifierId,
                    ParentId = SelectedParentId,
                    UnitId = SelectedUnitId,
                    IsAssessment = IsAssessment,
                    Coefficient = Coefficient,
                };

                await _service.CreateAsync(entity);
                EntityId = entity.Id;
                _physicalVolumeCache.Upsert(entity);
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

public sealed class AssessmentOption
{
    public bool? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}

public sealed class ClassifierDisplayItem
{
    public int? Id { get; set; }
    public string Display { get; set; } = string.Empty;
}

public sealed class ParentDisplayItem
{
    public int? Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
