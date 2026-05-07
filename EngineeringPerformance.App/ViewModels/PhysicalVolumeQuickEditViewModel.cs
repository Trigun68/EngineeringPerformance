using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PhysicalVolumeQuickEditViewModel : ObservableObject
{
    private readonly IDirectoryService<PhysicalVolume> _service;
    private readonly IDirectoryCache<PhysicalVolume> _pvCache;
    private readonly IDirectoryCache<IicWorkDirection> _classifierCache;
    private int _entityId;

    [ObservableProperty] private string _nomenclature = string.Empty;
    [ObservableProperty] private decimal _coefficient = 1m;
    [ObservableProperty] private bool? _isAssessment;
    [ObservableProperty] private int? _selectedClassifierId;
    [ObservableProperty] private ClassifierDisplayItem? _selectedClassifierItem;
    [ObservableProperty] private AssessmentOption? _selectedAssessmentItem;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading = true;

    partial void OnSelectedClassifierItemChanged(ClassifierDisplayItem? value)
        => SelectedClassifierId = value?.Id;

    partial void OnSelectedAssessmentItemChanged(AssessmentOption? value)
        => IsAssessment = value?.Value;

    public ObservableCollection<AssessmentOption> AssessmentOptions { get; } =
    [
        new AssessmentOption { Value = null, Display = "Не выбрано" },
        new AssessmentOption { Value = true,  Display = "Да" },
        new AssessmentOption { Value = false, Display = "Нет" },
    ];

    public ObservableCollection<ClassifierDisplayItem> ClassifierDisplayItems { get; } = [];

    public PhysicalVolumeQuickEditViewModel(
        IDirectoryService<PhysicalVolume> service,
        IDirectoryCache<PhysicalVolume> pvCache,
        IDirectoryCache<IicWorkDirection> classifierCache)
    {
        _service = service;
        _pvCache = pvCache;
        _classifierCache = classifierCache;
    }

    public void Prepare(int physicalVolumeId, string nomenclature, int? classifierId, decimal coefficient, bool? isAssessment)
    {
        _entityId = physicalVolumeId;
        Nomenclature = nomenclature;
        Coefficient = coefficient;
        SelectedClassifierId = classifierId;
        IsAssessment = isAssessment;
        ErrorMessage = null;
        IsLoading = true;
    }

    public async Task LoadLookupsAsync()
    {
        try
        {
            var classifiers = await _classifierCache.GetAllAsync();
            BuildClassifierDisplayItems(classifiers);
            SelectedClassifierItem = ClassifierDisplayItems.FirstOrDefault(i => i.Id == SelectedClassifierId)
                                     ?? ClassifierDisplayItems.FirstOrDefault();
            SelectedAssessmentItem = AssessmentOptions.FirstOrDefault(o => o.Value == IsAssessment)
                                     ?? AssessmentOptions[0];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildClassifierDisplayItems(IReadOnlyList<IicWorkDirection> classifiers)
    {
        ClassifierDisplayItems.Clear();
        ClassifierDisplayItems.Add(new ClassifierDisplayItem { Id = null, Display = "Не выбрано" });
        var byParent = classifiers.OrderBy(c => c.Code).ToLookup(c => c.ParentId);
        var visited = new HashSet<int>();
        foreach (var root in byParent[null])
            Append(root, byParent, 0, visited);
    }

    private void Append(IicWorkDirection item, ILookup<int?, IicWorkDirection> byParent, int depth, HashSet<int> visited)
    {
        if (!visited.Add(item.Id)) return;
        ClassifierDisplayItems.Add(new ClassifierDisplayItem
        {
            Id = item.Id,
            Display = new string(' ', depth * 3) + (depth > 0 ? "└ " : "") + $"{item.Code} {item.Name}",
        });
        foreach (var child in byParent[item.Id])
            Append(child, byParent, depth + 1, visited);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var all = await _pvCache.GetAllAsync();
            var entity = all.FirstOrDefault(p => p.Id == _entityId);
            if (entity is null)
            {
                ErrorMessage = "Запись не найдена";
                return;
            }
            entity.ClassifierId = SelectedClassifierId;
            entity.Coefficient = Coefficient;
            entity.IsAssessment = IsAssessment;
            await _service.UpdateAsync(entity);
            _pvCache.Upsert(entity);
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
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                window.DialogResult = result;
        });
    }
}
