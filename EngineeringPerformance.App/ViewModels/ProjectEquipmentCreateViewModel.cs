using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectEquipmentCreateViewModel : ObservableObject
{
    private readonly IDataTableService<ProjectEquipment> _service;
    private readonly IDirectoryCache<Equipment> _equipmentCache;

    private int _projectId;
    private int _versionId;

    [ObservableProperty] private string _windowTitle = "Добавить строку";
    [ObservableProperty] private bool _isLoading = true;

    [ObservableProperty] private int? _selectedEquipmentId;

    [ObservableProperty] private string? _facilityCode;
    [ObservableProperty] private string? _facilityName;
    [ObservableProperty] private string? _documentSet;
    [ObservableProperty] private string? _catalogId;
    [ObservableProperty] private string? _tag;
    [ObservableProperty] private string? _catalogName;
    [ObservableProperty] private string? _estimationBasis;
    [ObservableProperty] private decimal _quantity;

    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<EquipmentDisplayItem> EquipmentDisplayItems { get; } = [];

    public ProjectEquipmentCreateViewModel(
        IDataTableService<ProjectEquipment> service,
        IDirectoryCache<Equipment> equipmentCache)
    {
        _service = service;
        _equipmentCache = equipmentCache;
    }

    public void Prepare(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        _projectId = projectId;
        _versionId = versionId;
        WindowTitle = forCopy ? "Добавить строку копированием выбранной" : "Добавить строку";
        ErrorMessage = null;
        IsLoading = true;

        if (forCopy && source is not null)
        {
            FacilityCode = source.FacilityCode;
            FacilityName = source.FacilityName;
            DocumentSet = source.DocumentSet;
            CatalogId = source.CatalogId;
            Tag = source.Tag;
            CatalogName = source.PvName;
            EstimationBasis = source.EstimationBasis;
            Quantity = source.Quantity;
            SelectedEquipmentId = source.EquipmentId > 0 ? source.EquipmentId : null;
        }
        else
        {
            ResetFields();
        }
    }

    public void Prepare(bool forCopy, FactDetailRow? source, int projectId, int versionId)
    {
        _projectId = projectId;
        _versionId = versionId;
        WindowTitle = forCopy ? "Добавить строку копированием выбранной" : "Добавить строку";
        ErrorMessage = null;
        IsLoading = true;

        if (forCopy && source is not null)
        {
            FacilityCode = source.FacilityCode;
            FacilityName = source.FacilityName;
            DocumentSet = source.DocumentSet;
            CatalogId = source.CatalogId;
            Tag = source.Tag;
            CatalogName = source.PvName;
            EstimationBasis = source.EstimationBasis;
            Quantity = source.Quantity;
            SelectedEquipmentId = source.EquipmentId > 0 ? source.EquipmentId : null;
        }
        else
        {
            ResetFields();
        }
    }

    private void ResetFields()
    {
        FacilityCode = null;
        FacilityName = null;
        DocumentSet = null;
        CatalogId = null;
        Tag = null;
        CatalogName = null;
        EstimationBasis = null;
        Quantity = 0m;
        SelectedEquipmentId = null;
    }

    public async Task LoadLookupsAsync()
    {
        try
        {
            var all = await _equipmentCache.GetAllAsync();
            BuildEquipmentDisplayItems(all);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildEquipmentDisplayItems(IReadOnlyList<Equipment> items)
    {
        EquipmentDisplayItems.Clear();

        var byParent = items
            .Where(e => !e.IsArchived)
            .OrderBy(e => e.Name)
            .ToLookup(e => e.ParentId);

        var visited = new HashSet<int>();
        foreach (var root in byParent[null])
            Append(root, byParent, 0, visited);
    }

    private void Append(Equipment item, ILookup<int?, Equipment> byParent, int depth, HashSet<int> visited)
    {
        if (!visited.Add(item.Id)) return;
        EquipmentDisplayItems.Add(new EquipmentDisplayItem
        {
            Id = item.Id,
            Display = new string(' ', depth * 3) + (depth > 0 ? "└ " : string.Empty) + item.Name,
        });
        foreach (var child in byParent[item.Id])
            Append(child, byParent, depth + 1, visited);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedEquipmentId is not int equipmentId)
        {
            ErrorMessage = "Выберите номенклатуру оборудования";
            return;
        }
        if (Quantity <= 0m)
        {
            ErrorMessage = "Количество должно быть больше нуля";
            return;
        }

        try
        {
            var entity = new ProjectEquipment
            {
                ProjectId = _projectId,
                ProjectAnalysisVersionId = _versionId,
                EquipmentId = equipmentId,
                Quantity = Quantity,
                FacilityCode = FacilityCode,
                FacilityName = FacilityName,
                DocumentSet = DocumentSet,
                CatalogId = CatalogId,
                Tag = Tag,
                CatalogName = CatalogName,
                EstimationBasis = EstimationBasis,
            };
            await _service.CreateAsync(entity);
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

public sealed class EquipmentDisplayItem
{
    public int Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
