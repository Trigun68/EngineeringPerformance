using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectPhysicalVolumeCreateViewModel : ObservableObject
{
    private readonly IDataTableService<ProjectPhysicalVolume> _service;
    private readonly IDirectoryCache<PhysicalVolumeKfo> _kfoCache;
    private readonly IDirectoryCache<PhysicalVolumeBer> _berCache;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    private int _projectId;
    private int _versionId;
    private bool _forCopy;
    private bool _isKfo = true;

    [ObservableProperty] private string _windowTitle = "Добавить строку";
    [ObservableProperty] private bool _isLoading = true;

    [ObservableProperty] private int? _selectedPhysicalVolumeId;

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
        IDirectoryCache<PhysicalVolumeKfo> kfoCache,
        IDirectoryCache<PhysicalVolumeBer> berCache,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _service = service;
        _kfoCache = kfoCache;
        _berCache = berCache;
        _contextFactory = contextFactory;
    }

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
            ResetFields();
        }
    }

    public void Prepare(bool forCopy, FactDetailRow? source, int projectId, int versionId)
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
            ResetFields();
        }
    }

    private void ResetFields()
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

    public async Task LoadLookupsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            _isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(context, _versionId);

            if (_isKfo)
            {
                var all = await _kfoCache.GetAllAsync();
                BuildPhysicalVolumeDisplayItems(all.Select(v => (v.Id, v.Code, v.Name, v.ParentId, v.IsArchived)).ToList());
            }
            else
            {
                var all = await _berCache.GetAllAsync();
                BuildPhysicalVolumeDisplayItems(all.Select(v => (v.Id, v.Code, v.Name, v.ParentId, v.IsArchived)).ToList());
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildPhysicalVolumeDisplayItems(IReadOnlyList<(int Id, string Code, string Name, int? ParentId, bool IsArchived)> volumes)
    {
        PhysicalVolumeDisplayItems.Clear();

        var byParent = volumes
            .Where(v => !v.IsArchived)
            .OrderBy(v => v.Code)
            .ToLookup(v => v.ParentId);

        var visited = new HashSet<int>();
        foreach (var root in byParent[null])
            Append(root, byParent, 0, visited);
    }

    private void Append(
        (int Id, string Code, string Name, int? ParentId, bool IsArchived) item,
        ILookup<int?, (int Id, string Code, string Name, int? ParentId, bool IsArchived)> byParent,
        int depth,
        HashSet<int> visited)
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
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, pvId, _isKfo);
            await _service.CreateAsync(entity);
            CloseDialog(true);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            ErrorMessage = "Строка с такой комбинацией «номенклатура ФО + код/наименование объекта + ID работы» " +
                           "в данной версии проекта уже существует. Измените номенклатуру в выпадающем списке " +
                           "либо отредактируйте поля «Код объекта», «Наименование объекта» или «ID работы».";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        for (var current = (Exception?)ex; current is not null; current = current.InnerException)
        {
            var msg = current.Message;
            if (msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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
    public int Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
