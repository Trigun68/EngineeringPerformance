using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectPhysicalVolumeCreateViewModel : ObservableObject
{
    private readonly IDataTableService<ProjectPhysicalVolume> _service;
    private readonly IDirectoryCache<PhysicalVolume> _pvCache;

    private int _projectId;
    private int _versionId;
    private bool _forCopy;

    [ObservableProperty] private string _windowTitle = "Добавить строку";
    [ObservableProperty] private bool _isLoading = true;

    [ObservableProperty] private int? _selectedPhysicalVolumeId;
    [ObservableProperty] private PhysicalVolumeDisplayItem? _selectedPhysicalVolumeItem;

    [ObservableProperty] private string? _facilityCode;
    [ObservableProperty] private string? _facilityName;
    [ObservableProperty] private string? _bindingType;
    [ObservableProperty] private string? _documentSet;
    [ObservableProperty] private string? _catalogId;
    [ObservableProperty] private string? _tag;
    [ObservableProperty] private string? _workId;
    [ObservableProperty] private string? _workName;
    [ObservableProperty] private string? _workComment;
    [ObservableProperty] private string? _estimationBasis;
    [ObservableProperty] private decimal _quantity;

    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<PhysicalVolumeDisplayItem> PhysicalVolumeDisplayItems { get; } = [];

    public ProjectPhysicalVolumeCreateViewModel(
        IDataTableService<ProjectPhysicalVolume> service,
        IDirectoryCache<PhysicalVolume> pvCache)
    {
        _service = service;
        _pvCache = pvCache;
    }

    partial void OnSelectedPhysicalVolumeItemChanged(PhysicalVolumeDisplayItem? value)
        => SelectedPhysicalVolumeId = value?.Id;

    public void Prepare(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        _projectId = projectId;
        _versionId = versionId;
        _forCopy = forCopy;
        WindowTitle = forCopy ? "Добавить строку копированием выбранной" : "Добавить строку";
        ErrorMessage = null;
        IsLoading = true;

        if (forCopy && source is not null)
        {
            FacilityCode = source.FacilityCode;
            FacilityName = source.FacilityName;
            BindingType = source.BindingType;
            DocumentSet = source.DocumentSet;
            CatalogId = source.CatalogId;
            Tag = source.Tag;
            WorkId = source.WorkId;
            WorkName = source.WorkName;
            WorkComment = source.WorkComment;
            EstimationBasis = source.EstimationBasis;
            Quantity = source.Quantity;
            SelectedPhysicalVolumeId = source.PhysicalVolumeId;
        }
        else
        {
            FacilityCode = null;
            FacilityName = null;
            BindingType = null;
            DocumentSet = null;
            CatalogId = null;
            Tag = null;
            WorkId = null;
            WorkName = null;
            WorkComment = null;
            EstimationBasis = null;
            Quantity = 0m;
            SelectedPhysicalVolumeId = null;
        }
    }

    public async Task LoadLookupsAsync()
    {
        try
        {
            var all = await _pvCache.GetAllAsync();
            BuildPhysicalVolumeDisplayItems(all);
            SelectedPhysicalVolumeItem = PhysicalVolumeDisplayItems.FirstOrDefault(i => i.Id == SelectedPhysicalVolumeId)
                                         ?? PhysicalVolumeDisplayItems.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildPhysicalVolumeDisplayItems(IReadOnlyList<PhysicalVolume> volumes)
    {
        PhysicalVolumeDisplayItems.Clear();
        PhysicalVolumeDisplayItems.Add(new PhysicalVolumeDisplayItem { Id = null, Display = "Не выбрано" });

        var byParent = volumes
            .Where(v => !v.IsArchived)
            .OrderBy(v => v.Code)
            .ToLookup(v => v.ParentId);

        var visited = new HashSet<int>();
        foreach (var root in byParent[null])
            Append(root, byParent, 0, visited);
    }

    private void Append(PhysicalVolume item, ILookup<int?, PhysicalVolume> byParent, int depth, HashSet<int> visited)
    {
        if (!visited.Add(item.Id)) return;
        PhysicalVolumeDisplayItems.Add(new PhysicalVolumeDisplayItem
        {
            Id = item.Id,
            Display = new string(' ', depth * 3) + (depth > 0 ? "└ " : string.Empty) + $"{item.Code} {item.Name}",
        });
        foreach (var child in byParent[item.Id])
            Append(child, byParent, depth + 1, visited);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPhysicalVolumeId is not int pvId)
        {
            ErrorMessage = "Выберите номенклатуру физ. объёма";
            return;
        }
        if (Quantity <= 0m)
        {
            ErrorMessage = "Количество должно быть больше нуля";
            return;
        }

        try
        {
            var entity = new ProjectPhysicalVolume
            {
                ProjectId = _projectId,
                ProjectAnalysisVersionId = _versionId,
                PhysicalVolumeId = pvId,
                Quantity = Quantity,
                FacilityCode = FacilityCode,
                FacilityName = FacilityName,
                BindingType = BindingType,
                DocumentSet = DocumentSet,
                CatalogId = CatalogId,
                Tag = Tag,
                WorkId = WorkId,
                WorkName = WorkName,
                WorkComment = WorkComment,
                EstimationBasis = EstimationBasis,
            };
            await _service.CreateAsync(entity);
            CloseDialog(true);
        }
        catch (Exception ex)
        {
            // Возможные причины: нарушение уникального индекса (ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId)
            // или иные FK ограничения. Пользователю нужно сообщить точный текст СУБД для диагностики.
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

public sealed class PhysicalVolumeDisplayItem
{
    public int? Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
