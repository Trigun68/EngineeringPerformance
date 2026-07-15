using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectsPageViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectPickerDialogService _projectPickerDialog;
    private readonly IProjectDataDialogService _projectDataDialog;
    private readonly IProjectCharacteristicsDialogService _characteristicsDialog;
    private readonly IProjectParticipantsDialogService _participantsDialog;
    private readonly IProjectSelectionDialogService _projectSelectionDialog;
    private readonly IProjectVersionPickerDialogService _versionPickerDialog;
    private readonly IProjectAnalysisVersionQueryService _versionQueryService;
    private readonly IPerformanceService _performanceService;
    private readonly IDataImportDialogService _dataImportDialog;
    private readonly IIterationDeleteDialogService _iterationDeleteDialog;
    private readonly IConfirmDialogService _confirmDialog;
    private readonly IProjectPhysicalVolumeCreateDialogService _ppvCreateDialog;
    private readonly IProjectEquipmentCreateDialogService _peqCreateDialog;
    private readonly IDataTableService<ProjectPhysicalVolume> _ppvService;
    private readonly IDataTableService<ProjectEquipment> _peqService;
    private readonly IEquipmentEditDialogService _equipmentEditDialog;
    private readonly IProjectVersionAnalysisExportService _exportService;
    private readonly IProjectForecastExportService _forecastExportService;
    private readonly IProjectForecastService _forecastService;
    private readonly ProjectCharacteristicSelectionService _characteristicSelectionService;
    private readonly IDirectoryService<Project> _projectService;
    private readonly IDirectoryService<PhysicalVolumeKfo> _physicalVolumeKfoService;
    private readonly IDirectoryService<PhysicalVolumeBer> _physicalVolumeBerService;
    private readonly IDirectoryService<Equipment> _equipmentService;
    private readonly ProjectVersionGridDataService _gridDataService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserActionLogService _actionLog;
    private readonly CurrentUserHolder _currentUser;

    public Window? OwnerWindow { get; set; }

    // Выбранный проект
    [ObservableProperty] private int? _selectedProjectId;
    [ObservableProperty] private string _selectedProjectCode = string.Empty;
    [ObservableProperty] private string _selectedProjectName = string.Empty;
    [ObservableProperty] private decimal? _projectDeviationPercent;
    [ObservableProperty] private decimal? _accuracyScore;
    [ObservableProperty] private bool _hasProject;

    // Вкладка Прогнозирование — выбранная версия (план)
    [ObservableProperty] private int? _planVersionId;
    [ObservableProperty] private string _planVersionName = string.Empty;
    [ObservableProperty] private string _planVersionLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<ForecastPerformanceItem> _forecastData = [];
    [ObservableProperty] private ObservableCollection<FacilityForecastItem> _facilityForecastData = [];
    [ObservableProperty] private ObservableCollection<DetailForecastItem> _detailForecastData = [];

    // Вкладка Оценка факта — выбранная версия (факт)
    [ObservableProperty] private int? _factVersionId;
    [ObservableProperty] private string _factVersionName = string.Empty;
    [ObservableProperty] private string _factVersionLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<PerformanceItem> _factPerformanceData = [];
    [ObservableProperty] private ObservableCollection<FactFacilityRow> _factFacilityData = [];
    [ObservableProperty] private ObservableCollection<FactDetailRow> _factDetailData = [];

    // Вкладка Сравнение версий
    [ObservableProperty] private int? _compareVersionAId;
    [ObservableProperty] private string _compareVersionAName = string.Empty;
    [ObservableProperty] private string _compareVersionALabel = string.Empty;
    [ObservableProperty] private int? _compareVersionBId;
    [ObservableProperty] private string _compareVersionBName = string.Empty;
    [ObservableProperty] private string _compareVersionBLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<VersionCompareRow> _compareRows = [];
    [ObservableProperty] private ObservableCollection<VersionCompareFacilityRow> _compareFacilityData = [];
    [ObservableProperty] private ObservableCollection<VersionCompareDetailRow> _compareDetailData = [];
    [ObservableProperty] private bool _compareShowOnlyDifferences;
    [ObservableProperty] private bool _hasCompareData;

    // Полные данные для фильтрации
    private List<VersionCompareRow> _compareRowsFull = [];
    private List<VersionCompareFacilityRow> _compareFacilityFull = [];
    private List<VersionCompareDetailRow> _compareDetailFull = [];

    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public ProjectsPageViewModel(
        IProjectPickerDialogService projectPickerDialog,
        IProjectDataDialogService projectDataDialog,
        IProjectCharacteristicsDialogService characteristicsDialog,
        IProjectParticipantsDialogService participantsDialog,
        IProjectSelectionDialogService projectSelectionDialog,
        IProjectVersionPickerDialogService versionPickerDialog,
        IProjectAnalysisVersionQueryService versionQueryService,
        IPerformanceService performanceService,
        IProjectForecastService forecastService,
        IDataImportDialogService dataImportDialog,
        IIterationDeleteDialogService iterationDeleteDialog,
        IConfirmDialogService confirmDialog,
        IProjectPhysicalVolumeCreateDialogService ppvCreateDialog,
        IProjectEquipmentCreateDialogService peqCreateDialog,
        IDataTableService<ProjectPhysicalVolume> ppvService,
        IDataTableService<ProjectEquipment> peqService,
        IEquipmentEditDialogService equipmentEditDialog,
        IProjectVersionAnalysisExportService exportService,
        IProjectForecastExportService forecastExportService,
        ProjectCharacteristicSelectionService characteristicSelectionService,
        IDirectoryService<Project> projectService,
        IDirectoryService<PhysicalVolumeKfo> physicalVolumeKfoService,
        IDirectoryService<PhysicalVolumeBer> physicalVolumeBerService,
        IDirectoryService<Equipment> equipmentService,
        ProjectVersionGridDataService gridDataService,
        IServiceProvider serviceProvider,
        IUserActionLogService actionLog,
        CurrentUserHolder currentUser)
    {
        _projectPickerDialog = projectPickerDialog;
        _projectDataDialog = projectDataDialog;
        _characteristicsDialog = characteristicsDialog;
        _participantsDialog = participantsDialog;
        _projectSelectionDialog = projectSelectionDialog;
        _versionPickerDialog = versionPickerDialog;
        _versionQueryService = versionQueryService;
        _performanceService = performanceService;
        _forecastService = forecastService;
        _dataImportDialog = dataImportDialog;
        _iterationDeleteDialog = iterationDeleteDialog;
        _confirmDialog = confirmDialog;
        _ppvCreateDialog = ppvCreateDialog;
        _peqCreateDialog = peqCreateDialog;
        _ppvService = ppvService;
        _peqService = peqService;
        _equipmentEditDialog = equipmentEditDialog;
        _exportService = exportService;
        _forecastExportService = forecastExportService;
        _characteristicSelectionService = characteristicSelectionService;
        _projectService = projectService;
        _physicalVolumeKfoService = physicalVolumeKfoService;
        _physicalVolumeBerService = physicalVolumeBerService;
        _equipmentService = equipmentService;
        _gridDataService = gridDataService;
        _serviceProvider = serviceProvider;
        _actionLog = actionLog;
        _currentUser = currentUser;
    }

    public Func<DetailGridState>? SaveDetailForecastStateCallback { get; set; }
    public Action<DetailGridState>? RestoreDetailForecastStateCallback { get; set; }

    public Func<DetailGridState>? SaveFactDetailStateCallback { get; set; }
    public Action<DetailGridState>? RestoreFactDetailStateCallback { get; set; }

    public Task LoadAsync() => Task.CompletedTask;
    private Window? GetOwner() => OwnerWindow ?? Application.Current.MainWindow;

    private void SetDialogOwners()
    {
        var owner = GetOwner();
        if (owner is null) return;
        _projectPickerDialog.SetOwnerWindow(owner);
        _projectDataDialog.SetOwnerWindow(owner);
        _characteristicsDialog.SetOwnerWindow(owner);
        _participantsDialog.SetOwnerWindow(owner);
        _projectSelectionDialog.SetOwnerWindow(owner);
        _versionPickerDialog.SetOwnerWindow(owner);
        _dataImportDialog.SetOwnerWindow(owner);
        _iterationDeleteDialog.SetOwnerWindow(owner);
        _equipmentEditDialog.SetOwnerWindow(owner);
    }

    [RelayCommand]
    private async Task SelectProjectAsync()
    {
        SetDialogOwners();
        var projectId = await _projectPickerDialog.ShowPickerAsync();
        if (projectId is not int pid) return;
        await LoadProjectAsync(pid);
    }

    private async Task LoadProjectAsync(int projectId)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return;

        SelectedProjectId = projectId;
        SelectedProjectCode = project.Code;
        SelectedProjectName = project.Name;
        HasProject = true;

        PlanVersionId = null;
        PlanVersionName = string.Empty;
        PlanVersionLabel = string.Empty;
        ForecastData = [];
        FacilityForecastData = [];
        DetailForecastData = [];

        FactVersionId = null;
        FactVersionName = string.Empty;
        FactVersionLabel = string.Empty;
        FactPerformanceData = [];
        FactFacilityData = [];
        FactDetailData = [];

        CompareVersionAId = null;
        CompareVersionAName = string.Empty;
        CompareVersionALabel = string.Empty;
        CompareVersionBId = null;
        CompareVersionBName = string.Empty;
        CompareVersionBLabel = string.Empty;
        CompareRows = [];
        CompareFacilityData = [];
        CompareDetailData = [];
        CompareShowOnlyDifferences = false;
        HasCompareData = false;
        _compareRowsFull = [];
        _compareFacilityFull = [];
        _compareDetailFull = [];

        await LoadDeviationAsync(projectId);
    }

    private async Task LoadDeviationAsync(int projectId)
    {
        var rows = await _characteristicSelectionService.GetByProjectAsync(projectId);
        var values = rows.Where(r => r.ValuePercent.HasValue).Select(r => r.ValuePercent!.Value).ToList();
        ProjectDeviationPercent = values.Count == 0 ? null : Math.Round(values.Average(), 2, MidpointRounding.AwayFromZero);
    }

    [RelayCommand]
    private async Task OpenProjectDataAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();

        var project = await _projectService.GetByIdAsync(pid);
        if (project is null) return;
        var saved = await _projectDataDialog.ShowAsync(project);
        if (saved) await LoadProjectAsync(pid);
    }

    [RelayCommand]
    private async Task OpenCharacteristicsAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        var saved = await _characteristicsDialog.ShowAsync(pid);
        if (saved) await LoadDeviationAsync(pid);
    }

    [RelayCommand]
    private async Task OpenParticipantsAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        await _participantsDialog.ShowAsync(pid);
    }

    // ─── Прогнозирование ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectPlanVersionAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        var versionId = await _versionPickerDialog.ShowPickerAsync(pid, SelectedProjectName, AnalysisDirection.Plan);
        if (versionId is not int vid) return;

        PlanVersionId = vid;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        var ver = versions.FirstOrDefault(v => v.Id == vid);
        PlanVersionName = ver?.VersionName ?? string.Empty;
        PlanVersionLabel = BuildVersionLabel(ver);
        await LoadForecastDataAsync(pid, vid);
    }

    private async Task LoadForecastDataAsync(int projectId, int versionId, DetailGridState? savedState = null)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var perfTask = _performanceService.GetPerformanceDataAsync(projectId, versionId);
            var gridTask = _gridDataService.LoadAsync(
                projectId,
                versionId,
                new ProjectVersionGridLoadOptions { IncludeUnitsOnDirections = true });

            await Task.WhenAll(perfTask, gridTask);
            var data = perfTask.Result;
            var grid = gridTask.Result;
            var classifierMap = grid.ClassifierMap;
            var planningZByDirection = grid.PlanningZByDirection;
            var physicalVolumes = grid.AssessmentPhysicalVolumes;
            var physicalVolumesAll = grid.AllPhysicalVolumes;
            var equipmentAll = grid.AllEquipment;
            var snapshotNormIdByDirection = grid.SnapshotNormIdByDirection;

            // Ед. изм. для «Итого» — из справочника групп ФО IIC.
            // Сначала берём собственную ед. изм. группы (Unit.Name), как в окне справочника IicWorkDirections.
            // Только если у группы нет своей ед. изм. — наследуем от ближайшего предка вверх по дереву.
            string ResolveIicGroupUnitName(int workDirectionId)
            {
                if (workDirectionId < 0 || !classifierMap.TryGetValue(workDirectionId, out var dir))
                    return string.Empty;
                if (dir.Unit?.Name is string ownName && !string.IsNullOrWhiteSpace(ownName))
                    return ownName.Trim();
                return IicWorkDirectionDisplayUnit.ResolveUnitName(dir, classifierMap) ?? string.Empty;
            }

            // ── Вкладка «Итого»: иерархия направлений ──────────────────────
            var childrenByParent = data
                .Where(r => r.WorkDirectionId >= 0 && r.ParentId.HasValue)
                .ToLookup(r => r.ParentId!.Value);
            var roots = data
                .Where(r => r.WorkDirectionId >= 0 && !r.ParentId.HasValue)
                .ToList();

            var result = new List<ForecastPerformanceItem>();

            (decimal? baseH, decimal? optH, decimal? pesH) CalcHoursFromRow(PerformanceRowDto row)
            {
                var vol = row.FactPhysicalVolume ?? 0m;
                if (vol <= 0 || !row.NormProductivity.HasValue)
                    return (null, null, null);
                return ForecastCalculation.ComputeForecastHours(
                    vol,
                    row.NormProductivity,
                    row.NormStdDev,
                    ForecastCalculation.ResolvePlanningZ(planningZByDirection, row.WorkDirectionId));
            }

            (decimal baseH, decimal optH, decimal pesH) BuildSubtree(PerformanceRowDto row, int level)
            {
                var (ownBase, ownOpt, ownPes) = CalcHoursFromRow(row);
                var children = childrenByParent[row.WorkDirectionId].ToList();
                bool hasChildren = children.Count > 0;

                var indent = new string(' ', level * 4);
                result.Add(new ForecastPerformanceItem
                {
                    Id = row.WorkDirectionId,
                    Code = row.WorkDirectionCode,
                    Name = row.WorkDirectionName,
                    DisplayName = indent + row.WorkDirectionName,
                    NormUnitName = ResolveIicGroupUnitName(row.WorkDirectionId),
                    FactPhysicalVolume = row.FactPhysicalVolume,
                    NormProductivity = row.NormProductivity,
                    BaseHours = ownBase,
                    OptimisticHours = ownOpt,
                    PessimisticHours = ownPes,
                    IsTotal = false,
                    Level = level,
                });

                if (!hasChildren)
                    return (ownBase ?? 0m, ownOpt ?? 0m, ownPes ?? 0m);

                decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
                foreach (var child in children)
                {
                    var (cb, co, cp) = BuildSubtree(child, level + 1);
                    totalBase += cb;
                    totalOpt += co;
                    totalPes += cp;
                }

                var aggBase = (ownBase ?? 0m) + totalBase;
                var aggOpt  = (ownOpt  ?? 0m) + totalOpt;
                var aggPes  = (ownPes  ?? 0m) + totalPes;

                result.Add(new ForecastPerformanceItem
                {
                    Id = row.WorkDirectionId,
                    Name = $"Итого: {row.WorkDirectionName}",
                    DisplayName = indent + $"Итого: {row.WorkDirectionName}",
                    NormUnitName = ResolveIicGroupUnitName(row.WorkDirectionId),
                    FactPhysicalVolume = null,
                    NormProductivity = null,
                    BaseHours = aggBase > 0 ? aggBase : null,
                    OptimisticHours = aggOpt > 0 ? aggOpt : null,
                    PessimisticHours = aggPes > 0 ? aggPes : null,
                    IsTotal = true,
                    Level = level,
                });

                return (aggBase, aggOpt, aggPes);
            }

            foreach (var root in roots)
                BuildSubtree(root, 0);

            var unallocated = data.FirstOrDefault(r => r.WorkDirectionId < 0);
            if (unallocated is not null)
            {
                var (ub, uo, up) = CalcHoursFromRow(unallocated);
                result.Add(new ForecastPerformanceItem
                {
                    Id = unallocated.WorkDirectionId,
                    Name = unallocated.WorkDirectionName,
                    DisplayName = unallocated.WorkDirectionName,
                    NormUnitName = string.Empty,
                    FactPhysicalVolume = unallocated.FactPhysicalVolume,
                    NormProductivity = null,
                    BaseHours = ub,
                    OptimisticHours = uo,
                    PessimisticHours = up,
                    IsTotal = false,
                    Level = 0,
                });
            }

            // Итоговая строка вкладки «Итого» — сумма корневых итогов
            var rootTotals = result.Where(r => r.IsTotal && r.Level == 0).ToList();
            if (rootTotals.Count > 0)
            {
                result.Add(new ForecastPerformanceItem
                {
                    Id = int.MinValue,
                    Name = "ИТОГО",
                    DisplayName = "ИТОГО",
                    NormUnitName = string.Empty,
                    FactPhysicalVolume = null,
                    NormProductivity = null,
                    BaseHours        = rootTotals.Sum(r => r.BaseHours ?? 0m),
                    OptimisticHours  = rootTotals.Sum(r => r.OptimisticHours ?? 0m),
                    PessimisticHours = rootTotals.Sum(r => r.PessimisticHours ?? 0m),
                    IsTotal = true,
                    Level = -1,
                });
            }

            // Итоги только для корневых элементов (Level=0) и общий итог (Level=-1)
            ForecastData = new ObservableCollection<ForecastPerformanceItem>(
                result.Where(r => !r.IsTotal || r.Level <= 0));

            // ── Вкладка «По объектам»: агрегат по FacilityCode ─────────────
            var normByDirection = data
                .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
                .ToDictionary(r => r.WorkDirectionId);

            var byFacility = physicalVolumes
                .GroupBy(pv => (
                    Code: pv.FacilityCode ?? string.Empty,
                    Name: pv.FacilityName ?? string.Empty))
                .OrderBy(g => g.Key.Code)
                .ThenBy(g => g.Key.Name);

            var facilityResult = new List<FacilityForecastItem>();
            foreach (var facilityGroup in byFacility)
            {
                decimal baseH = 0m, optH = 0m, pesH = 0m;
                foreach (var pv in facilityGroup)
                {
                    var dirId = pv.GetClassifierId()!.Value;
                    if (!normByDirection.TryGetValue(dirId, out var norm))
                        continue;

                    var vol = pv.Quantity * pv.GetCoefficient();
                    var (fb, fo, fp) = ForecastCalculation.ComputeForecastHours(
                        vol, norm.NormProductivity, norm.NormStdDev,
                        ForecastCalculation.ResolvePlanningZ(planningZByDirection, dirId));
                    baseH += fb ?? 0m;
                    optH += fo ?? 0m;
                    pesH += fp ?? 0m;
                }

                foreach (var eq in equipmentAll.Where(e =>
                             (e.FacilityCode ?? string.Empty) == facilityGroup.Key.Code
                             && (e.FacilityName ?? string.Empty) == facilityGroup.Key.Name))
                {
                    var dirId = eq.Equipment?.ClassifierId;
                    if (eq.Equipment?.IsAssessment != true || !dirId.HasValue)
                        continue;
                    if (!normByDirection.TryGetValue(dirId.Value, out var norm))
                        continue;

                    var vol = eq.QuantityInIicGroupUnits();
                    var (eb, eo, ep) = ForecastCalculation.ComputeForecastHours(
                        vol, norm.NormProductivity, norm.NormStdDev,
                        ForecastCalculation.ResolvePlanningZ(planningZByDirection, dirId.Value));
                    baseH += eb ?? 0m;
                    optH += eo ?? 0m;
                    pesH += ep ?? 0m;
                }

                facilityResult.Add(new FacilityForecastItem
                {
                    FacilityCode = facilityGroup.Key.Code,
                    FacilityName = facilityGroup.Key.Name,
                    BaseHours        = baseH > 0 ? Math.Round(baseH, 2) : null,
                    OptimisticHours  = optH  > 0 ? Math.Round(optH,  2) : null,
                    PessimisticHours = pesH  > 0 ? Math.Round(pesH,  2) : null,
                });
            }

            FacilityForecastData = new ObservableCollection<FacilityForecastItem>(facilityResult);

            // ── Вкладка «Детально»: одна строка БД → одна строка таблицы ──
            var allPv = physicalVolumesAll
                .OrderBy(pv => pv.FacilityCode ?? string.Empty)
                .ThenBy(pv => pv.FacilityName ?? string.Empty)
                .ThenBy(pv => pv.GetVolumeCode());

            var detailRows = new List<DetailForecastItem>();
            foreach (var pv in allPv)
            {
                var dirId = pv.GetClassifierId();
                PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var nd) ? nd : null;

                var isAssessment = pv.GetIsAssessment() == true;
                var vol = pv.Quantity * (pv.GetCoefficient());
                decimal? baseH = null, optH = null, pesH = null;
                if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                {
                    (baseH, optH, pesH) = ForecastCalculation.ComputeForecastHours(
                        vol, p, norm.NormStdDev,
                        ForecastCalculation.ResolvePlanningZ(planningZByDirection, dirId ?? -1));
                }

                detailRows.Add(new DetailForecastItem
                {
                    ProjectPhysicalVolumeId = pv.Id,
                    PhysicalVolumeId      = pv.GetCatalogVolumeId() ?? 0,
                    ClassifierId          = pv.GetClassifierId(),
                    NormId                = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nid) ? nid : null,
                    FacilityCode          = pv.FacilityCode ?? string.Empty,
                    FacilityName          = pv.FacilityName ?? string.Empty,
                    BindingType           = pv.BindingType ?? string.Empty,
                    DocumentSet           = pv.DocumentSet ?? string.Empty,
                    CatalogId             = pv.CatalogId ?? string.Empty,
                    Tag                   = pv.Tag ?? string.Empty,
                    WorkId                = pv.WorkId ?? string.Empty,
                    WorkName              = pv.WorkName ?? string.Empty,
                    ItemName              = pv.WorkName ?? string.Empty,
                    WorkComment           = pv.WorkComment ?? string.Empty,
                    EstimationBasis       = pv.EstimationBasis ?? string.Empty,
                    PvCode                = pv.GetVolumeCode(),
                    PvName                = pv.GetVolumeName(),
                    IsAssessment          = isAssessment ? "Да" : "Нет",
                    Coefficient           = pv.GetCoefficient(),
                    IicGroupName          = pv.GetClassifierId() is int cid && classifierMap.TryGetValue(cid, out var cl)
                        ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, classifierMap)
                        : string.Empty,
                    IicUnitName           = IicWorkDirectionDisplayUnit.ResolveProjectPhysicalVolumeUnitName(pv) ?? string.Empty,
                    NormProductivity      = norm?.NormProductivity,
                    NormStdDev            = norm?.NormStdDev,
                    Quantity              = pv.Quantity,
                    BaseHours             = baseH,
                    OptimisticHours       = optH,
                    PessimisticHours      = pesH,
                });
            }

            foreach (var eq in equipmentAll
                         .OrderBy(e => e.FacilityCode ?? string.Empty)
                         .ThenBy(e => e.FacilityName ?? string.Empty)
                         .ThenBy(e => e.Equipment?.Name ?? string.Empty))
            {
                var dirId = eq.Equipment?.ClassifierId;
                PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var ndEq)
                    ? ndEq
                    : null;
                var isAssessment = eq.Equipment?.IsAssessment == true;
                var vol = eq.QuantityInIicGroupUnits();
                decimal? baseH = null, optH = null, pesH = null;
                if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                {
                    (baseH, optH, pesH) = ForecastCalculation.ComputeForecastHours(
                        vol, p, norm.NormStdDev,
                        ForecastCalculation.ResolvePlanningZ(planningZByDirection, dirId ?? -1));
                }

                detailRows.Add(new DetailForecastItem
                {
                    ProjectPhysicalVolumeId = 0,
                    PhysicalVolumeId = 0,
                    ProjectEquipmentId = eq.Id,
                    EquipmentId = eq.EquipmentId,
                    ClassifierId = dirId,
                    NormId = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nidEq) ? nidEq : null,
                    FacilityCode = eq.FacilityCode ?? string.Empty,
                    FacilityName = eq.FacilityName ?? string.Empty,
                    DocumentSet = eq.DocumentSet ?? string.Empty,
                    CatalogId = eq.CatalogId ?? string.Empty,
                    Tag = eq.Tag ?? string.Empty,
                    PvCode = eq.CatalogId ?? string.Empty,
                    PvName = eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                    ItemName = eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                    IsAssessment = isAssessment ? "Да" : "Нет",
                    Coefficient = eq.Equipment?.Coefficient,
                    IicGroupName = dirId is int cidEq && classifierMap.TryGetValue(cidEq, out var clEq)
                        ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(clEq, classifierMap)
                        : string.Empty,
                    IicUnitName = norm?.NormUnitName ?? string.Empty,
                    NormProductivity = norm?.NormProductivity,
                    NormStdDev = norm?.NormStdDev,
                    Quantity = eq.Quantity,
                    BaseHours = baseH,
                    OptimisticHours = optH,
                    PessimisticHours = pesH,
                });
            }

            DetailForecastData = new ObservableCollection<DetailForecastItem>(detailRows);
            if (savedState is not null)
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    () => RestoreDetailForecastStateCallback?.Invoke(savedState),
                    System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных прогноза: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportPlanPhysicalVolumesAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.PhysicalVolume);
        if (result is { Success: true }) await LoadForecastDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task DeletePlanPhysicalVolumesAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.PhysicalVolume);
        if (result) await LoadForecastDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task ImportPlanEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.Equipment);
        if (result is { Success: true }) await LoadForecastDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task DeletePlanEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.Equipment);
        if (result) await LoadForecastDataAsync(pid, vid);
    }

    [RelayCommand]
    private void OpenForecastReport()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid)
        {
            ErrorMessage = "Выберите версию плана для просмотра прогноза";
            return;
        }

        var owner = GetOwner();
        if (owner is null) return;

        var window = _serviceProvider.GetRequiredService<Views.ProjectForecastReportWindow>();
        var vm = _serviceProvider.GetRequiredService<ProjectForecastReportViewModel>();
        vm.ProjectId = pid;
        vm.VersionId = vid;
        vm.ProjectNameWithVersion = string.IsNullOrWhiteSpace(PlanVersionName)
            ? SelectedProjectName
            : $"{SelectedProjectName} ({PlanVersionName})";
        vm.SetForecastData(ForecastData, SelectedProjectCode, PlanVersionName);
        vm.OwnerWindow = window;
        window.DataContext = vm;
        window.Owner = owner;
        window.ShowDialog();
    }

    // ─── Оценка факта ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectFactVersionAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        var versionId = await _versionPickerDialog.ShowPickerAsync(pid, SelectedProjectName, AnalysisDirection.Fact);
        if (versionId is not int vid) return;

        FactVersionId = vid;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        var ver = versions.FirstOrDefault(v => v.Id == vid);
        FactVersionName = ver?.VersionName ?? string.Empty;
        FactVersionLabel = BuildVersionLabel(ver);
        await LoadFactDataAsync(pid, vid);
    }

    private static string BuildVersionLabel(ProjectAnalysisVersion? ver)
    {
        if (ver is null) return string.Empty;
        var date = ver.CreatedAt.ToString("dd.MM.yyyy");
        var label = $"({date}) {ver.VersionName}";
        if (!string.IsNullOrWhiteSpace(ver.Description))
            label += $": {ver.Description}";
        return label;
    }

    private async Task LoadFactDataAsync(int projectId, int versionId, DetailGridState? savedState = null)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var perfTask = _performanceService.GetPerformanceDataAsync(projectId, versionId);
            var gridTask = _gridDataService.LoadAsync(
                projectId,
                versionId,
                new ProjectVersionGridLoadOptions
                {
                    IncludeLaborIntensities = true,
                    IncludeActualHoursInLabor = true,
                });

            await Task.WhenAll(perfTask, gridTask);
            var data = perfTask.Result ?? throw new InvalidOperationException("Performance data is null");
            var grid = gridTask.Result;
            var classifierMap = grid.ClassifierMap;
            var physicalVolumes = grid.AssessmentPhysicalVolumes;
            var physicalVolumesAll = grid.AllPhysicalVolumes;
            var equipmentAll = grid.AllEquipment;
            var snapshotNormIdByDirection = grid.SnapshotNormIdByDirection;
            var laborIntensitiesRaw = grid.LaborIntensities;
            // Связка LI ↔ PV: трудоёмкость относится к комплекту документов (DocumentSet)
            // или марке (Tag). LI.DocumentationCode хранится как «<тип документации>/<название>|<DocumentSet>»
            // (например `AC_03Схема ... |4L0000-XX-XX2`), а в PV есть прямая колонка DocumentSet.
            // Соответствие найдено: 32 из 33 уникальных PV.DocumentSet точно совпадают с хвостами LI.
            // Извлекаем хвост после «|» — это и есть DocumentSet. Если «|» нет, считаем строку
            // не идентифицированной (пойдёт в строку «(пусто)»).
            static string ExtractDocSet(string? code)
            {
                if (string.IsNullOrEmpty(code)) return string.Empty;
                var pipeIdx = code.IndexOf('|');
                return pipeIdx >= 0 ? code[(pipeIdx + 1)..].Trim() : string.Empty;
            }
            var laborIntensities = laborIntensitiesRaw
                .Select(li => new FactLaborIntensityRow(
                    ExtractDocSet(li.DocumentationCode),
                    li.WorkDirectionId,
                    // Часы для агрегации Факта берутся строго из PlannedHours (заявленные в файле
                    // плановые часы по работе). Прежняя смесь `Actual > 0 ? Actual : Planned`
                    // давала итог > файла на строках, где факт списан больше плана (для проекта 4 v2
                    // это +755.84 ч к 61 725.40). Решено пользователем 2026-05-12: единый источник —
                    // PlannedHours, фактическое списание (ActualHours) для агрегации не используется.
                    li.PlannedHours))
                .ToList();

            // ── Вкладка «Итого»: иерархия направлений (текущая логика) ──────
            var items = new ObservableCollection<PerformanceItem>();
            foreach (var row in data)
            {
                items.Add(new PerformanceItem
                {
                    Id = row.WorkDirectionId,
                    ParentId = row.ParentId,
                    Code = row.WorkDirectionCode,
                    Name = row.WorkDirectionName,
                    NormUnitName = row.NormUnitName ?? string.Empty,
                    FactPhysicalVolume = row.FactPhysicalVolume,
                    FactLaborHours = row.FactLaborHours,
                    FactProductivity = row.FactProductivity,
                    FactEstimate = row.FactEstimate,
                    NormProductivity = row.NormProductivity,
                    NormEstimate = row.NormEstimate,
                    NormStdDev = row.NormStdDev,
                    ZScore = row.ZScore,
                    Percentile = PerformancePercentileHelper.GetPercentile(row.ZScore),
                    Interpretation = row.Interpretation?.ToRussian() ?? string.Empty,
                });
            }
            FactPerformanceData = items;

            // ── Вкладки «Детально» и «По объектам» ────────────────────────
            var normByDirection = data
                .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
                .GroupBy(r => r.WorkDirectionId)
                .ToDictionary(g => g.Key, g => g.First());

            var (detailRows, facilityRows) = ComputeFactRows(
                physicalVolumesAll,
                equipmentAll,
                laborIntensities,
                normByDirection,
                snapshotNormIdByDirection,
                classifierMap);

            FactDetailData = new ObservableCollection<FactDetailRow>(detailRows);
            FactFacilityData = new ObservableCollection<FactFacilityRow>(facilityRows);

            if (savedState is not null)
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    () => RestoreFactDetailStateCallback?.Invoke(savedState),
                    System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных оценки: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Объединяет данные по объектам из двух версий для сравнения.
    /// </summary>
    private static List<VersionCompareFacilityRow> BuildCompareFacilityRows(
        IReadOnlyList<FactFacilityRow> facilityA,
        IReadOnlyList<FactFacilityRow> facilityB)
    {
        var dictA = facilityA
            .GroupBy(f => f.Code)
            .ToDictionary(g => g.Key, g => g.First());
        var dictB = facilityB
            .GroupBy(f => f.Code)
            .ToDictionary(g => g.Key, g => g.First());
        var allCodes = dictA.Keys.Union(dictB.Keys).OrderBy(c => c);

        var result = new List<VersionCompareFacilityRow>();
        foreach (var code in allCodes)
        {
            var hasA = dictA.TryGetValue(code, out var rowA);
            var hasB = dictB.TryGetValue(code, out var rowB);

            var hoursA = rowA?.FactLaborHours;
            var hoursB = rowB?.FactLaborHours;
            var hoursDelta = (hoursA.HasValue && hoursB.HasValue) ? hoursB.Value - hoursA.Value : (decimal?)null;

            var zScoreA = rowA?.ZScore;
            var zScoreB = rowB?.ZScore;
            var zDelta = (zScoreA.HasValue && zScoreB.HasValue) ? zScoreB.Value - zScoreA.Value : (decimal?)null;

            result.Add(new VersionCompareFacilityRow
            {
                FacilityCode = code,
                FacilityName = rowB?.Name ?? rowA?.Name ?? string.Empty,
                FactLaborHoursA = hoursA,
                FactLaborHoursB = hoursB,
                HoursDelta = hoursDelta,
                ZScoreA = zScoreA,
                ZScoreB = zScoreB,
                ZDelta = zDelta,
                InterpretationA = rowA?.Interpretation ?? string.Empty,
                InterpretationB = rowB?.Interpretation ?? string.Empty,
            });
        }

        return result;
    }

    /// <summary>
    /// Объединяет детальные данные из двух версий для сравнения.
    /// Ключ объединения: (FacilityCode, DocumentSet, PvCode, WorkId).
    /// </summary>
    private static List<VersionCompareDetailRow> BuildCompareDetailRows(
        IReadOnlyList<FactDetailRow> detailA,
        IReadOnlyList<FactDetailRow> detailB)
    {
        static decimal? AverageProductivity(IEnumerable<FactDetailRow> rows)
        {
            var values = rows
                .Where(d => d.FactProductivity.HasValue)
                .Select(d => d.FactProductivity!.Value)
                .ToList();
            return values.Count > 0 ? values.Average() : null;
        }

        // Группируем по ключу и суммируем (на случай дубликатов)
        var groupedA = detailA
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageProductivity(g)));

        var groupedB = detailB
            .GroupBy(d => (d.FacilityCode, d.DocumentSet, d.PvCode, d.WorkId))
            .ToDictionary(
                g => g.Key,
                g => (
                    First: g.First(),
                    Quantity: g.Sum(d => d.Quantity),
                    FactLaborHours: g.Sum(d => d.FactLaborHours ?? 0m),
                    FactProductivity: AverageProductivity(g)));

        var allKeys = groupedA.Keys.Union(groupedB.Keys)
            .OrderBy(k => k.FacilityCode)
            .ThenBy(k => k.DocumentSet)
            .ThenBy(k => k.PvCode)
            .ThenBy(k => k.WorkId);

        var result = new List<VersionCompareDetailRow>();
        foreach (var key in allKeys)
        {
            var hasA = groupedA.TryGetValue(key, out var rowA);
            var hasB = groupedB.TryGetValue(key, out var rowB);

            var quantityA = hasA ? (decimal?)rowA!.Quantity : null;
            var quantityB = hasB ? (decimal?)rowB!.Quantity : null;
            var quantityDelta = (quantityA.HasValue && quantityB.HasValue) ? quantityB.Value - quantityA.Value : (decimal?)null;

            var hoursA = hasA && rowA!.FactLaborHours > 0 ? (decimal?)Math.Round(rowA.FactLaborHours, 2) : null;
            var hoursB = hasB && rowB!.FactLaborHours > 0 ? (decimal?)Math.Round(rowB.FactLaborHours, 2) : null;
            var hoursDelta = (hoursA.HasValue && hoursB.HasValue) ? hoursB.Value - hoursA.Value : (decimal?)null;

            var prodA = hasA ? rowA!.FactProductivity : null;
            var prodB = hasB ? rowB!.FactProductivity : null;
            var prodDelta = (prodA.HasValue && prodB.HasValue) ? prodB.Value - prodA.Value : (decimal?)null;

            var presence = (hasA && hasB) ? "A+B" : hasA ? "только A" : "только B";

            var refRow = hasB ? rowB.First : hasA ? rowA.First : null;

            result.Add(new VersionCompareDetailRow
            {
                FacilityCode = key.FacilityCode,
                FacilityName = refRow?.FacilityName ?? string.Empty,
                DocumentSet = key.DocumentSet,
                PvCode = key.PvCode,
                PvName = refRow?.PvName ?? string.Empty,
                WorkId = key.WorkId,
                WorkName = refRow?.WorkName ?? string.Empty,
                QuantityA = quantityA,
                QuantityB = quantityB,
                QuantityDelta = quantityDelta,
                FactLaborHoursA = hoursA,
                FactLaborHoursB = hoursB,
                HoursDelta = hoursDelta,
                FactProductivityA = prodA,
                FactProductivityB = prodB,
                ProductivityDelta = prodDelta,
                IicGroupName = refRow?.IicGroupName ?? string.Empty,
                IicUnitName = refRow?.IicUnitName ?? string.Empty,
                Presence = presence,
            });
        }

        return result;
    }

    /// <summary>
    /// Вычисляет детальные строки и строки по объектам на основе физических объёмов и трудоёмкости.
    /// Связка: LI.DocumentSet (хвост DocumentationCode после «|») ↔ PV.DocumentSet.
    /// Распределение: внутри (DocSet, Direction) часы LI делятся между PPV
    /// пропорционально объёму (Quantity × Coefficient) только assessment-строк.
    /// </summary>
    private static (List<FactDetailRow> DetailRows, List<FactFacilityRow> FacilityRows) ComputeFactRows(
        List<ProjectPhysicalVolume> physicalVolumesAll,
        List<ProjectEquipment> equipmentAll,
        IReadOnlyList<FactLaborIntensityRow> laborIntensities,
        Dictionary<int, PerformanceRowDto> normByDirection,
        Dictionary<int, int> snapshotNormIdByDirection,
        Dictionary<int, IicWorkDirection> classifierMap)
    {
        var hoursByDocSetDir = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue && !string.IsNullOrEmpty(li.DocSet))
            .GroupBy(li => (Set: li.DocSet, Dir: li.WorkDirectionId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Hours));

        // Объёмы PV (assessment) по (DocSet, Direction) — знаменатель для пропорции.
        var docSetVolumes = physicalVolumesAll
            .Where(pv => pv.GetIsAssessment() == true
                      && pv.GetClassifierId().HasValue
                      && !string.IsNullOrEmpty(pv.DocumentSet))
            .GroupBy(pv => (
                Set: pv.DocumentSet!,
                Dir: pv.GetClassifierId()!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.Quantity * pv.GetCoefficient()));

        // Сначала строим «Детально» (рассчитываем factHours по каждому PPV),
        // затем агрегируем суммы факт-часов в «По объектам» по FacilityCode.
        var allPv = physicalVolumesAll
            .OrderBy(pv => pv.FacilityCode ?? string.Empty)
            .ThenBy(pv => pv.FacilityName ?? string.Empty)
            .ThenBy(pv => pv.GetVolumeCode());

        var detailRows = new List<FactDetailRow>();
        decimal totalMatchedHours = 0m;
        foreach (var pv in allPv)
        {
            var dirId = pv.GetClassifierId();
            PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var nd) ? nd : null;

            var isAssessment = pv.GetIsAssessment() == true;
            var vol = pv.Quantity * (pv.GetCoefficient());

            decimal? factHours = null;
            decimal? factProductivity = null;
            decimal? zScore = null;
            string percentile = "Н/Д";
            string interpretationText = string.Empty;

            if (isAssessment && dirId.HasValue && vol > 0 && !string.IsNullOrEmpty(pv.DocumentSet))
            {
                var key = (Set: pv.DocumentSet!, Dir: dirId.Value);
                if (docSetVolumes.TryGetValue(key, out var setVol)
                    && setVol > 0
                    && hoursByDocSetDir.TryGetValue(key, out var setHours)
                    && setHours > 0)
                {
                    factHours = Math.Round(setHours * (vol / setVol), 2);
                    totalMatchedHours += factHours.Value;
                    factProductivity = Math.Round(setHours / setVol, 4);
                    if (factProductivity > 0
                        && norm?.NormEstimate is decimal ne
                        && norm.NormStdDev is decimal nsd
                        && nsd > 0)
                    {
                        var factEstimate = (decimal)Math.Log((double)factProductivity.Value);
                        zScore = Math.Round((factEstimate - ne) / nsd, 4);
                        percentile = PerformancePercentileHelper.GetPercentile(zScore);
                        interpretationText = PerformanceInterpretationHelper.GetInterpretation(zScore)?.ToRussian() ?? string.Empty;
                    }
                }
            }

            detailRows.Add(new FactDetailRow
            {
                ProjectPhysicalVolumeId = pv.Id,
                PhysicalVolumeId      = pv.GetCatalogVolumeId() ?? 0,
                NormId                = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nid2) ? nid2 : null,
                FacilityCode          = pv.FacilityCode ?? string.Empty,
                FacilityName          = pv.FacilityName ?? string.Empty,
                BindingType           = pv.BindingType ?? string.Empty,
                DocumentSet           = pv.DocumentSet ?? string.Empty,
                CatalogId             = pv.CatalogId ?? string.Empty,
                Tag                   = pv.Tag ?? string.Empty,
                WorkId                = pv.WorkId ?? string.Empty,
                    WorkName              = pv.WorkName ?? string.Empty,
                    ItemName              = pv.WorkName ?? string.Empty,
                    WorkComment           = pv.WorkComment ?? string.Empty,
                EstimationBasis       = pv.EstimationBasis ?? string.Empty,
                PvCode                = pv.GetVolumeCode(),
                PvName                = pv.GetVolumeName(),
                IicGroupName          = pv.GetClassifierId() is int cid2 && classifierMap.TryGetValue(cid2, out var cl2) ? $"{cl2.Code} {cl2.Name}" : string.Empty,
                IicUnitName           = norm?.NormUnitName ?? string.Empty,
                Quantity              = pv.Quantity,
                FactLaborHours        = factHours,
                FactProductivity      = factProductivity,
                NormProductivity      = norm?.NormProductivity,
                ZScore                = zScore,
                Percentile            = percentile,
                Interpretation        = interpretationText,
            });
        }

        foreach (var eq in equipmentAll
                     .OrderBy(e => e.FacilityCode ?? string.Empty)
                     .ThenBy(e => e.FacilityName ?? string.Empty)
                     .ThenBy(e => e.Equipment?.Name ?? string.Empty))
        {
            var dirId = eq.Equipment?.ClassifierId;
            PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var ndEq)
                ? ndEq
                : null;
            var isAssessment = eq.Equipment?.IsAssessment == true;
            var vol = eq.QuantityInIicGroupUnits();
            decimal? factHours = null;
            if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                factHours = Math.Round(vol * p, 2);

            detailRows.Add(new FactDetailRow
            {
                ProjectPhysicalVolumeId = 0,
                PhysicalVolumeId = 0,
                ProjectEquipmentId = eq.Id,
                EquipmentId = eq.EquipmentId,
                NormId = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nidEq) ? nidEq : null,
                FacilityCode = eq.FacilityCode ?? string.Empty,
                FacilityName = eq.FacilityName ?? string.Empty,
                DocumentSet = eq.DocumentSet ?? string.Empty,
                CatalogId = eq.CatalogId ?? string.Empty,
                Tag = eq.Tag ?? string.Empty,
                PvCode = eq.CatalogId ?? string.Empty,
                PvName = eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                ItemName = eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                IicGroupName = dirId is int cidEq && classifierMap.TryGetValue(cidEq, out var clEq)
                    ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(clEq, classifierMap)
                    : string.Empty,
                IicUnitName = norm?.NormUnitName ?? string.Empty,
                Quantity = eq.Quantity,
                FactLaborHours = factHours,
                NormProductivity = norm?.NormProductivity,
            });
        }

        // ── Агрегация «По объектам» из detailRows ──────────────────────
        // Группировка по FacilityCode (дедуп FacilityName), сумма factHours,
        // Z-score = взвешенная по часам (часы одинаковы для всех PPV одного DocSet+Direction,
        // поэтому фактически усреднение идёт по направлениям внутри объекта).
        var facilityResult = detailRows
            .Where(r => !string.IsNullOrEmpty(r.FacilityCode))
            .GroupBy(r => r.FacilityCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                var hoursRows = g.Where(r => r.FactLaborHours.HasValue && r.ZScore.HasValue).ToList();
                decimal? totalHours = g.Any(r => r.FactLaborHours.HasValue)
                    ? Math.Round(g.Sum(r => r.FactLaborHours ?? 0m), 2)
                    : (decimal?)null;
                decimal? facilityZ = null;
                if (hoursRows.Count > 0)
                {
                    var sumHours = hoursRows.Sum(r => r.FactLaborHours!.Value);
                    if (sumHours > 0)
                    {
                        var weighted = hoursRows.Sum(r => r.ZScore!.Value * r.FactLaborHours!.Value);
                        facilityZ = Math.Round(weighted / sumHours, 4);
                    }
                }
                var interp = PerformanceInterpretationHelper.GetInterpretation(facilityZ);
                return new FactFacilityRow
                {
                    Code = g.Key,
                    Name = first.FacilityName,
                    FactLaborHours = totalHours,
                    ZScore = facilityZ,
                    Percentile = PerformancePercentileHelper.GetPercentile(facilityZ),
                    Interpretation = interp?.ToRussian() ?? string.Empty,
                };
            })
            .ToList();

        // ── Строка «(пусто)»: не идентифицированная трудоёмкость ────────
        // Вся сумма LI минус то, что удалось распределить по PPV через DocSet.
        // Попадают сюда: часы LI без DocumentSet; часы с DocSet, которому в PV
        // нет ни одной assessment-строки; остатки округлений.
        var totalLiHours = laborIntensities.Sum(li => li.Hours);
        var unmatchedHours = Math.Round(totalLiHours - totalMatchedHours, 2);
        if (unmatchedHours > 0)
        {
            facilityResult.Add(new FactFacilityRow
            {
                Code = string.Empty,
                Name = "(пусто)",
                FactLaborHours = unmatchedHours,
                ZScore = null,
                Percentile = "Н/Д",
                Interpretation = string.Empty,
            });
            detailRows.Add(new FactDetailRow
            {
                ProjectPhysicalVolumeId = 0,
                FacilityName = "(пусто)",
                FactLaborHours = unmatchedHours,
            });
        }

        return (detailRows, facilityResult);
    }

    /// <summary>
    /// Загружает данные факта для одной версии (для сравнения версий).
    /// </summary>
    private async Task<(List<FactDetailRow> Detail, List<FactFacilityRow> Facility)> LoadFactDataForCompareAsync(
        int projectId,
        int versionId)
    {
        var perfData = await _performanceService.GetPerformanceDataAsync(projectId, versionId);

        var grid = await _gridDataService.LoadAsync(
            projectId,
            versionId,
            new ProjectVersionGridLoadOptions { IncludeLaborIntensities = true });

        var classifierMap = grid.ClassifierMap;
        var physicalVolumesAll = grid.AllPhysicalVolumes;
        var equipmentAll = grid.AllEquipment;
        var snapshotNormIdByDirection = grid.SnapshotNormIdByDirection;
        var laborIntensitiesRaw = grid.LaborIntensities;

        static string ExtractDocSet(string? code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            var pipeIdx = code.IndexOf('|');
            return pipeIdx >= 0 ? code[(pipeIdx + 1)..].Trim() : string.Empty;
        }

        var laborIntensities = laborIntensitiesRaw
            .Select(li => new FactLaborIntensityRow(
                ExtractDocSet(li.DocumentationCode),
                li.WorkDirectionId,
                li.PlannedHours))
            .ToList();

        var normByDirection = perfData
            .Where(r => r.WorkDirectionId >= 0 && r.NormProductivity.HasValue)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        return ComputeFactRows(
            physicalVolumesAll,
            equipmentAll,
            laborIntensities,
            normByDirection,
            snapshotNormIdByDirection,
            classifierMap);
    }

    [RelayCommand]
    private async Task ImportFactPhysicalVolumesAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.PhysicalVolume);
        if (result is { Success: true }) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task DeleteFactPhysicalVolumesAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.PhysicalVolume);
        if (result) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task ImportFactEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.Equipment);
        if (result is { Success: true }) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task DeleteFactEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.Equipment);
        if (result) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task ImportLaborIntensitiesAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.LaborIntensity);
        if (result is { Success: true }) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private async Task DeleteLaborIntensitiesAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.LaborIntensity);
        if (result) await LoadFactDataAsync(pid, vid);
    }

    [RelayCommand]
    private void OpenVersionAnalysis()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid)
        {
            ErrorMessage = "Выберите версию для отчёта";
            return;
        }

        var owner = GetOwner();
        if (owner is null) return;

        var window = _serviceProvider.GetRequiredService<ProjectVersionAnalysisWindow>();
        var vm = _serviceProvider.GetRequiredService<ProjectVersionAnalysisViewModel>();
        vm.ProjectId = pid;
        vm.VersionId = vid;
        vm.OwnerWindow = window;
        window.DataContext = vm;
        window.Owner = owner;
        _ = vm.LoadAsync();
        window.ShowDialog();
    }

    // ─── Сравнение версий ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectCompareVersionAAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        // Разрешены и план, и факт — без фильтра по направлению
        var versionId = await _versionPickerDialog.ShowPickerAsync(pid, SelectedProjectName);
        if (versionId is not int vid) return;
        CompareVersionAId = vid;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        var ver = versions.FirstOrDefault(v => v.Id == vid);
        CompareVersionAName = ver?.VersionName ?? string.Empty;
        CompareVersionALabel = BuildVersionLabel(ver);
        await RunCompareAsync();
    }

    [RelayCommand]
    private async Task SelectCompareVersionBAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        // Разрешены и план, и факт — без фильтра по направлению
        var versionId = await _versionPickerDialog.ShowPickerAsync(pid, SelectedProjectName);
        if (versionId is not int vid) return;
        CompareVersionBId = vid;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        var ver = versions.FirstOrDefault(v => v.Id == vid);
        CompareVersionBName = ver?.VersionName ?? string.Empty;
        CompareVersionBLabel = BuildVersionLabel(ver);
        await RunCompareAsync();
    }

    [RelayCommand]
    private async Task ImportCompareEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || CompareVersionAId is not int vid) return;
        SetDialogOwners();
        var result = await _dataImportDialog.ShowImportDialogAsync(pid, vid, DataImportType.Equipment);
        if (result is { Success: true }) await RunCompareAsync();
    }

    [RelayCommand]
    private async Task DeleteCompareEquipmentAsync()
    {
        if (SelectedProjectId is not int pid || CompareVersionAId is not int vid) return;
        SetDialogOwners();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(pid, vid, DataImportType.Equipment);
        if (result) await RunCompareAsync();
    }

    private async Task RunCompareAsync()
    {
        if (SelectedProjectId is not int pid
            || CompareVersionAId is not int vidA
            || CompareVersionBId is not int vidB)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var dataA = await _performanceService.GetPerformanceDataAsync(pid, vidA);
            var dataB = await _performanceService.GetPerformanceDataAsync(pid, vidB);

            // ── Вкладка «Итого» ──────────────────────────────────────────────
            var dictA = dataA
                .GroupBy(r => r.WorkDirectionId)
                .ToDictionary(g => g.Key, g => g.First());
            var dictB = dataB
                .GroupBy(r => r.WorkDirectionId)
                .ToDictionary(g => g.Key, g => g.First());
            var allIds = dictA.Keys.Union(dictB.Keys).ToList();

            var items = new ObservableCollection<VersionCompareRow>();
            foreach (var id in allIds)
            {
                dictA.TryGetValue(id, out var a);
                dictB.TryGetValue(id, out var b);

                if (a is null && b is null)
                    continue;

                var row = a ?? b!;
                decimal? zDelta = a?.ZScore.HasValue == true && b?.ZScore.HasValue == true
                    ? b.ZScore - a.ZScore
                    : null;
                items.Add(new VersionCompareRow
                {
                    Id = row.WorkDirectionId,
                    ParentId = row.ParentId,
                    Name = row.WorkDirectionName,
                    ZScoreA = a?.ZScore,
                    ZScoreB = b?.ZScore,
                    ZDelta = zDelta,
                    InterpretationA = a?.Interpretation?.ToRussian() ?? "-",
                    InterpretationB = b?.Interpretation?.ToRussian() ?? "-",
                });
            }
            CompareRows = items;

            // ── Вкладки «По объектам» и «Детально» ───────────────────────────
            // Загружаем данные факта для обеих версий последовательно (правило из памяти)
            var (detailA, facilityA) = await LoadFactDataForCompareAsync(pid, vidA);
            var (detailB, facilityB) = await LoadFactDataForCompareAsync(pid, vidB);

            // Объединяем данные через методы слияния
            var compareFacilityRows = BuildCompareFacilityRows(facilityA, facilityB);
            var compareDetailRows = BuildCompareDetailRows(detailA, detailB);

            // Сохраняем полные данные и применяем фильтр
            _compareRowsFull = items.ToList();
            _compareFacilityFull = compareFacilityRows;
            _compareDetailFull = compareDetailRows;
            HasCompareData = true;
            ApplyCompareFilter();
        }
        catch (Exception ex)
        {
            var root = ex.InnerException ?? ex;
            ErrorMessage = $"Ошибка ({root.GetType().Name}): {root.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnCompareShowOnlyDifferencesChanged(bool value) => ApplyCompareFilter();

    private void ApplyCompareFilter()
    {
        if (!HasCompareData) return;

        CompareRows = new ObservableCollection<VersionCompareRow>(
            FilterCompareRows(_compareRowsFull, CompareShowOnlyDifferences));
        CompareFacilityData = new ObservableCollection<VersionCompareFacilityRow>(
            FilterCompareFacility(_compareFacilityFull, CompareShowOnlyDifferences));
        CompareDetailData = new ObservableCollection<VersionCompareDetailRow>(
            FilterCompareDetail(_compareDetailFull, CompareShowOnlyDifferences));
    }

    private static List<VersionCompareRow> FilterCompareRows(
        List<VersionCompareRow> rows,
        bool showOnlyDifferences)
    {
        if (!showOnlyDifferences) return rows;

        // Для TreeList нужно сохранить иерархию: показываем узел, если у него есть видимые потомки
        var visible = new HashSet<int>();

        // Сначала находим все листья с различиями
        foreach (var row in rows)
        {
            var isIdentical = Math.Round(row.ZScoreA ?? 0m, 4) == Math.Round(row.ZScoreB ?? 0m, 4)
                           && row.InterpretationA == row.InterpretationB;
            if (!isIdentical)
            {
                visible.Add(row.Id);
            }
        }

        // Затем добавляем всех предков видимых узлов
        bool changed;
        do
        {
            changed = false;
            foreach (var row in rows)
            {
                if (visible.Contains(row.Id) && row.ParentId.HasValue && !visible.Contains(row.ParentId.Value))
                {
                    visible.Add(row.ParentId.Value);
                    changed = true;
                }
            }
        } while (changed);

        return rows.Where(r => visible.Contains(r.Id)).ToList();
    }

    private static List<VersionCompareFacilityRow> FilterCompareFacility(
        List<VersionCompareFacilityRow> rows,
        bool showOnlyDifferences)
    {
        if (!showOnlyDifferences) return rows;

        return rows.Where(r =>
        {
            // Оба null = идентично (скрыть)
            if (!r.FactLaborHoursA.HasValue && !r.FactLaborHoursB.HasValue) return false;

            // Проверяем идентичность
            var hoursIdentical = Math.Round(r.FactLaborHoursA ?? 0m, 2) == Math.Round(r.FactLaborHoursB ?? 0m, 2);
            var zScoreIdentical = Math.Round(r.ZScoreA ?? 0m, 4) == Math.Round(r.ZScoreB ?? 0m, 4);

            return !(hoursIdentical && zScoreIdentical);
        }).ToList();
    }

    private static List<VersionCompareDetailRow> FilterCompareDetail(
        List<VersionCompareDetailRow> rows,
        bool showOnlyDifferences)
    {
        if (!showOnlyDifferences) return rows;

        return rows.Where(r =>
        {
            // Строки с Presence != "A+B" всегда (отсутствие = отклонение)
            if (r.Presence != "A+B") return true;

            // Проверяем идентичность
            var quantityIdentical = Math.Round(r.QuantityA ?? 0m, 4) == Math.Round(r.QuantityB ?? 0m, 4);
            var hoursIdentical = Math.Round(r.FactLaborHoursA ?? 0m, 2) == Math.Round(r.FactLaborHoursB ?? 0m, 2);
            var prodIdentical = Math.Round(r.FactProductivityA ?? 0m, 4) == Math.Round(r.FactProductivityB ?? 0m, 4);

            return !(quantityIdentical && hoursIdentical && prodIdentical);
        }).ToList();
    }

    // ─── Отчёты ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportFactAnalysisToExcelAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт анализа версии в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"АнализВерсии_{SelectedProjectCode}_{FactVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var svc = _serviceProvider.GetRequiredService<Core.Services.IProjectVersionAnalysisService>();
            var report = await svc.GenerateReportAsync(pid, vid);
            await _exportService.ExportToExcelAsync(report, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFactAnalysisToPdfAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт анализа версии в PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"АнализВерсии_{SelectedProjectCode}_{FactVersionName}.pdf",
            DefaultExt = "pdf",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var svc = _serviceProvider.GetRequiredService<Core.Services.IProjectVersionAnalysisService>();
            var report = await svc.GenerateReportAsync(pid, vid);
            await _exportService.ExportToPdfAsync(report, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportForecastToExcelAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт прогноза в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Прогноз_{SelectedProjectCode}_{PlanVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = ForecastData
                .Select(static r => r.ToExportDto())
                .ToList();
            await _forecastExportService.ExportForecastSummaryToExcelAsync(rows, SelectedProjectCode, PlanVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFacilityForecastToExcelAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт прогноза по объектам в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"ПрогнозПоОбъектам_{SelectedProjectCode}_{PlanVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = FacilityForecastData
                .Where(f => !f.IsTotal)
                .Select(f => new FacilityForecastRowDto(f.FacilityCode, f.FacilityName, f.BaseHours, f.OptimisticHours, f.PessimisticHours))
                .ToList();
            await _forecastExportService.ExportFacilityForecastToExcelAsync(rows, SelectedProjectCode, PlanVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFacilityForecastToPdfAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт прогноза по объектам в PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"ПрогнозПоОбъектам_{SelectedProjectCode}_{PlanVersionName}.pdf",
            DefaultExt = "pdf",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = FacilityForecastData
                .Where(f => !f.IsTotal)
                .Select(f => new FacilityForecastRowDto(f.FacilityCode, f.FacilityName, f.BaseHours, f.OptimisticHours, f.PessimisticHours))
                .ToList();
            await _forecastExportService.ExportFacilityForecastToPdfAsync(rows, SelectedProjectCode, PlanVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportDetailForecastToExcelAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт детального прогноза в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"ПрогнозДетально_{SelectedProjectCode}_{PlanVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = DetailForecastData
                .Where(r => !r.IsTotal)
                .Select(static r => r.ToExportDto())
                .ToList();
            await _forecastExportService.ExportDetailForecastToExcelAsync(
                rows, SelectedProjectCode, PlanVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportDetailForecastSummaryTableAsync()
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Сводная таблица прогноза",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"СводнаяТаблица_{SelectedProjectCode}_{PlanVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var directions = await _gridDataService.GetActiveDirectionNodesAsync();

            var inputRows = DetailForecastData
                .Where(r => !r.IsTotal)
                .Select(r => new DetailForecastSummaryInputRow(
                    r.FacilityCode,
                    r.FacilityName,
                    r.ClassifierId,
                    r.Quantity,
                    r.Coefficient,
                    r.BaseHours))
                .ToList();

            var table = DetailForecastSummaryTableBuilder.Build(
                inputRows,
                directions,
                SelectedProjectCode,
                PlanVersionName);

            await _forecastExportService.ExportDetailForecastSummaryTableToExcelAsync(table, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFactFacilityToExcelAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт факта по объектам в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"ФактПоОбъектам_{SelectedProjectCode}_{FactVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = FactFacilityData
                .Select(f => new FactFacilityRowDto(f.Code, f.Name, f.FactLaborHours, f.ZScore, f.Percentile, f.Interpretation))
                .ToList();
            await _forecastExportService.ExportFactFacilityToExcelAsync(rows, SelectedProjectCode, FactVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFactFacilityToPdfAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт факта по объектам в PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"ФактПоОбъектам_{SelectedProjectCode}_{FactVersionName}.pdf",
            DefaultExt = "pdf",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = FactFacilityData
                .Select(f => new FactFacilityRowDto(f.Code, f.Name, f.FactLaborHours, f.ZScore, f.Percentile, f.Interpretation))
                .ToList();
            await _forecastExportService.ExportFactFacilityToPdfAsync(rows, SelectedProjectCode, FactVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportFactDetailToExcelAsync()
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var dlg = new SaveFileDialog
        {
            Title = "Экспорт детального факта в Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"ФактДетально_{SelectedProjectCode}_{FactVersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var rows = FactDetailData
                .Select(r => new FactDetailRowDto(
                    r.FacilityCode, r.FacilityName, r.BindingType, r.DocumentSet,
                    r.CatalogId, r.Tag, r.WorkId, r.WorkName, r.WorkComment,
                    r.EstimationBasis, r.PvCode, r.PvName,
                    r.IicGroupName, r.IicUnitName, r.Quantity,
                    r.FactLaborHours, r.FactProductivity, r.NormProductivity,
                    r.ZScore, r.Percentile, r.Interpretation))
                .ToList();
            await _forecastExportService.ExportFactDetailToExcelAsync(
                rows, SelectedProjectCode, FactVersionName, dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── Контекстное меню «Детально» ──────────────────────────────────────

    [RelayCommand]
    private async Task EditProjectPhysicalVolumeAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0 || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<ProjectPhysicalVolumeQuickEditViewModel>();
        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        vm.Prepare(row.ProjectPhysicalVolumeId, nomenclature, row.Quantity);

        var window = _serviceProvider.GetRequiredService<Views.ProjectPhysicalVolumeQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        var state = SaveDetailForecastStateCallback?.Invoke();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.PhysicalVolumeEdit, "ProjectPhysicalVolume", row.ProjectPhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadForecastDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditPhysicalVolumeNomenclatureAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0 || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var state = SaveDetailForecastStateCallback?.Invoke();

        var vm = _serviceProvider.GetRequiredService<PhysicalVolumeQuickEditViewModel>();
        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        vm.Prepare(row.PhysicalVolumeId, nomenclature, row.NormId.HasValue ? null : (int?)null,
            row.Coefficient ?? 1m, row.IsAssessment == "Да" ? true : row.IsAssessment == "Нет" ? false : (bool?)null);

        var meta = await GetCatalogVolumeMetaAsync(row.PhysicalVolumeId);
        if (meta is not null)
            vm.Prepare(row.PhysicalVolumeId, nomenclature, meta.Value.ClassifierId, meta.Value.Coefficient, meta.Value.IsAssessment, meta.Value.IsKfo);

        var window = _serviceProvider.GetRequiredService<Views.PhysicalVolumeQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        _ = vm.LoadLookupsAsync();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.LaborIntensityEdit, "PhysicalVolume", row.PhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadForecastDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditIicNormAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || row.NormId is not int normId) return;

        var state = SaveDetailForecastStateCallback?.Invoke();
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<IicNormQuickEditViewModel>();
        await vm.PrepareAsync(normId);

        var window = _serviceProvider.GetRequiredService<Views.IicNormQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.IicNormEdit, "IicWorkDirectionNorm", normId, $"ProjectId={pid}, VersionId={vid}");
            await LoadForecastDataAsync(pid, vid, state);
        }
    }

    private async Task LogDetailEditAsync(UserActionType actionType, string entityType, int entityId, string description)
    {
        var userId = _currentUser.User?.Id;
        if (userId is null) return;
        await _actionLog.LogAsync(userId.Value, actionType, entityType: entityType, entityId: entityId, description: description);
    }

    [RelayCommand]
    private async Task DeleteProjectPhysicalVolumeAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0 || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        var confirmed = await _confirmDialog.ShowAsync(
            "Удалить строку",
            $"Удалить выбранную строку?\n{nomenclature}",
            "Удалить");
        if (!confirmed) return;

        var state = SaveDetailForecastStateCallback?.Invoke();
        try
        {
            await _ppvService.DeleteAsync(row.ProjectPhysicalVolumeId);
        }
        catch (Exception ex)
        {
            // FK от связанных сущностей (например ProjectLaborIntensity по PhysicalVolumeId не блокирует,
            // но возможны иные ограничения СУБД) — текст ошибки показываем пользователю как информационное окно.
            await _confirmDialog.ShowAsync(
                "Ошибка удаления",
                $"Не удалось удалить строку: {ex.Message}",
                "OK", "OK");
            return;
        }
        await LogDetailEditAsync(UserActionType.PhysicalVolumeDelete, "ProjectPhysicalVolume",
            row.ProjectPhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadForecastDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task CreateProjectPhysicalVolumeFromCopyAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0 || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        await ShowCreateDialogAsync(forCopy: true, source: row, pid, vid);
    }

    [RelayCommand]
    private async Task CreateProjectPhysicalVolumeAsync(DetailForecastItem? _)
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        await ShowCreateDialogAsync(forCopy: false, source: null, pid, vid);
    }

    private async Task ShowCreateDialogAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        _ppvCreateDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveDetailForecastStateCallback?.Invoke();
        var created = await _ppvCreateDialog.ShowAsync(forCopy, source, projectId, versionId);
        if (!created) return;

        var description = forCopy && source is not null
            ? $"ProjectId={projectId}, VersionId={versionId}, copyOf={source.ProjectPhysicalVolumeId}"
            : $"ProjectId={projectId}, VersionId={versionId}";
        await LogDetailEditAsync(UserActionType.PhysicalVolumeCreate, "ProjectPhysicalVolume", 0, description);
        await LoadForecastDataAsync(projectId, versionId, state);
    }

    // ─── Контекстное меню «Детально» — оборудование (Forecast) ────────────

    [RelayCommand]
    private async Task EditProjectEquipmentAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || !row.IsEquipmentRow || row.ProjectEquipmentId <= 0
            || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<ProjectEquipmentQuickEditViewModel>();
        var nomenclature = row.ItemName;
        vm.Prepare(row.ProjectEquipmentId, nomenclature, row.Quantity);

        var window = _serviceProvider.GetRequiredService<Views.ProjectEquipmentQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        var state = SaveDetailForecastStateCallback?.Invoke();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.EquipmentEdit, "ProjectEquipment", row.ProjectEquipmentId,
                $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadForecastDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditEquipmentNomenclatureAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || !row.IsEquipmentRow || row.EquipmentId <= 0
            || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var equipment = await _equipmentService.GetByIdAsync(row.EquipmentId);
        if (equipment is null) return;

        _equipmentEditDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveDetailForecastStateCallback?.Invoke();
        var nomenclature = row.ItemName;
        if (await _equipmentEditDialog.ShowEditDialogAsync(equipment) is null) return;

        await LogDetailEditAsync(UserActionType.EquipmentEdit, "Equipment", row.EquipmentId,
            $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadForecastDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task DeleteProjectEquipmentAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || !row.IsEquipmentRow || row.ProjectEquipmentId <= 0
            || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var nomenclature = row.ItemName;
        var confirmed = await _confirmDialog.ShowAsync(
            "Удалить строку",
            $"Удалить выбранную строку?\n{nomenclature}",
            "Удалить");
        if (!confirmed) return;

        var state = SaveDetailForecastStateCallback?.Invoke();
        try
        {
            await _peqService.DeleteAsync(row.ProjectEquipmentId);
        }
        catch (Exception ex)
        {
            await _confirmDialog.ShowAsync(
                "Ошибка удаления",
                $"Не удалось удалить строку: {ex.Message}",
                "OK", "OK");
            return;
        }
        await LogDetailEditAsync(UserActionType.EquipmentDelete, "ProjectEquipment",
            row.ProjectEquipmentId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadForecastDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task CreateProjectEquipmentFromCopyAsync(DetailForecastItem? row)
    {
        if (row is null || row.IsTotal || !row.IsEquipmentRow || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        await ShowEquipmentCreateDialogAsync(forCopy: true, source: row, pid, vid);
    }

    [RelayCommand]
    private async Task CreateProjectEquipmentAsync(DetailForecastItem? _)
    {
        if (SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
        await ShowEquipmentCreateDialogAsync(forCopy: false, source: null, pid, vid);
    }

    private async Task ShowEquipmentCreateDialogAsync(bool forCopy, DetailForecastItem? source, int projectId, int versionId)
    {
        _peqCreateDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveDetailForecastStateCallback?.Invoke();
        var created = await _peqCreateDialog.ShowAsync(forCopy, source, projectId, versionId);
        if (!created) return;

        var description = forCopy && source is not null
            ? $"ProjectId={projectId}, VersionId={versionId}, copyOf={source.ProjectEquipmentId}"
            : $"ProjectId={projectId}, VersionId={versionId}";
        await LogDetailEditAsync(UserActionType.EquipmentCreate, "ProjectEquipment", 0, description);
        await LoadForecastDataAsync(projectId, versionId, state);
    }

    // ─── Контекстное меню «Детально» (Fact) ───────────────────────────────

    [RelayCommand]
    private async Task EditFactProjectPhysicalVolumeAsync(FactDetailRow? row)
    {
        if (row is null || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<ProjectPhysicalVolumeQuickEditViewModel>();
        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        vm.Prepare(row.ProjectPhysicalVolumeId, nomenclature, row.Quantity);

        var window = _serviceProvider.GetRequiredService<Views.ProjectPhysicalVolumeQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        var state = SaveFactDetailStateCallback?.Invoke();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.PhysicalVolumeEdit, "ProjectPhysicalVolume", row.ProjectPhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadFactDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditFactPhysicalVolumeNomenclatureAsync(FactDetailRow? row)
    {
        if (row is null || row.IsEquipmentRow || row.PhysicalVolumeId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var state = SaveFactDetailStateCallback?.Invoke();

        var vm = _serviceProvider.GetRequiredService<PhysicalVolumeQuickEditViewModel>();
        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        vm.Prepare(row.PhysicalVolumeId, nomenclature, null, 1m, null);

        var meta = await GetCatalogVolumeMetaAsync(row.PhysicalVolumeId);
        if (meta is not null)
            vm.Prepare(row.PhysicalVolumeId, nomenclature, meta.Value.ClassifierId, meta.Value.Coefficient, meta.Value.IsAssessment, meta.Value.IsKfo);

        var window = _serviceProvider.GetRequiredService<Views.PhysicalVolumeQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        _ = vm.LoadLookupsAsync();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.LaborIntensityEdit, "PhysicalVolume", row.PhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadFactDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditFactIicNormAsync(FactDetailRow? row)
    {
        if (row is null || row.NormId is not int normId) return;

        var state = SaveFactDetailStateCallback?.Invoke();
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<IicNormQuickEditViewModel>();
        await vm.PrepareAsync(normId);

        var window = _serviceProvider.GetRequiredService<Views.IicNormQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.IicNormEdit, "IicWorkDirectionNorm", normId, $"ProjectId={pid}, VersionId={vid}");
            await LoadFactDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task DeleteFactProjectPhysicalVolumeAsync(FactDetailRow? row)
    {
        if (row is null || row.IsEquipmentRow || row.ProjectPhysicalVolumeId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        var confirmed = await _confirmDialog.ShowAsync(
            "Удалить строку",
            $"Удалить выбранную строку?\n{nomenclature}",
            "Удалить");
        if (!confirmed) return;

        var state = SaveFactDetailStateCallback?.Invoke();
        try
        {
            await _ppvService.DeleteAsync(row.ProjectPhysicalVolumeId);
        }
        catch (Exception ex)
        {
            await _confirmDialog.ShowAsync(
                "Ошибка удаления",
                $"Не удалось удалить строку: {ex.Message}",
                "OK", "OK");
            return;
        }
        await LogDetailEditAsync(UserActionType.PhysicalVolumeDelete, "ProjectPhysicalVolume",
            row.ProjectPhysicalVolumeId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadFactDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task CreateFactProjectPhysicalVolumeFromCopyAsync(FactDetailRow? row)
    {
        if (row is null || row.IsEquipmentRow || SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        await ShowFactCreateDialogAsync(forCopy: true, source: row, pid, vid);
    }

    [RelayCommand]
    private async Task CreateFactProjectPhysicalVolumeAsync(FactDetailRow? _)
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        await ShowFactCreateDialogAsync(forCopy: false, source: null, pid, vid);
    }

    private async Task ShowFactCreateDialogAsync(bool forCopy, FactDetailRow? source, int projectId, int versionId)
    {
        _ppvCreateDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveFactDetailStateCallback?.Invoke();
        var created = await _ppvCreateDialog.ShowAsync(forCopy, source, projectId, versionId);
        if (!created) return;

        var description = forCopy && source is not null
            ? $"ProjectId={projectId}, VersionId={versionId}, copyOf={source.ProjectPhysicalVolumeId}"
            : $"ProjectId={projectId}, VersionId={versionId}";
        await LogDetailEditAsync(UserActionType.PhysicalVolumeCreate, "ProjectPhysicalVolume", 0, description);
        await LoadFactDataAsync(projectId, versionId, state);
    }

    // ─── Контекстное меню «Детально» — оборудование (Fact) ────────────────

    [RelayCommand]
    private async Task EditFactProjectEquipmentAsync(FactDetailRow? row)
    {
        if (row is null || !row.IsEquipmentRow || row.ProjectEquipmentId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var vm = _serviceProvider.GetRequiredService<ProjectEquipmentQuickEditViewModel>();
        var nomenclature = row.ItemName;
        vm.Prepare(row.ProjectEquipmentId, nomenclature, row.Quantity);

        var window = _serviceProvider.GetRequiredService<Views.ProjectEquipmentQuickEditDialog>();
        window.DataContext = vm;
        window.Owner = GetOwner();
        var state = SaveFactDetailStateCallback?.Invoke();
        if (window.ShowDialog() == true)
        {
            await LogDetailEditAsync(UserActionType.EquipmentEdit, "ProjectEquipment", row.ProjectEquipmentId,
                $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
            await LoadFactDataAsync(pid, vid, state);
        }
    }

    [RelayCommand]
    private async Task EditFactEquipmentNomenclatureAsync(FactDetailRow? row)
    {
        if (row is null || !row.IsEquipmentRow || row.EquipmentId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var equipment = await _equipmentService.GetByIdAsync(row.EquipmentId);
        if (equipment is null) return;

        _equipmentEditDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveFactDetailStateCallback?.Invoke();
        var nomenclature = row.ItemName;
        if (await _equipmentEditDialog.ShowEditDialogAsync(equipment) is null) return;

        await LogDetailEditAsync(UserActionType.EquipmentEdit, "Equipment", row.EquipmentId,
            $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadFactDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task DeleteFactProjectEquipmentAsync(FactDetailRow? row)
    {
        if (row is null || !row.IsEquipmentRow || row.ProjectEquipmentId <= 0
            || SelectedProjectId is not int pid || FactVersionId is not int vid) return;

        var nomenclature = row.ItemName;
        var confirmed = await _confirmDialog.ShowAsync(
            "Удалить строку",
            $"Удалить выбранную строку?\n{nomenclature}",
            "Удалить");
        if (!confirmed) return;

        var state = SaveFactDetailStateCallback?.Invoke();
        try
        {
            await _peqService.DeleteAsync(row.ProjectEquipmentId);
        }
        catch (Exception ex)
        {
            await _confirmDialog.ShowAsync(
                "Ошибка удаления",
                $"Не удалось удалить строку: {ex.Message}",
                "OK", "OK");
            return;
        }
        await LogDetailEditAsync(UserActionType.EquipmentDelete, "ProjectEquipment",
            row.ProjectEquipmentId, $"ProjectId={pid}, VersionId={vid}, {nomenclature}");
        await LoadFactDataAsync(pid, vid, state);
    }

    [RelayCommand]
    private async Task CreateFactProjectEquipmentFromCopyAsync(FactDetailRow? row)
    {
        if (row is null || !row.IsEquipmentRow || SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        await ShowFactEquipmentCreateDialogAsync(forCopy: true, source: row, pid, vid);
    }

    [RelayCommand]
    private async Task CreateFactProjectEquipmentAsync(FactDetailRow? _)
    {
        if (SelectedProjectId is not int pid || FactVersionId is not int vid) return;
        await ShowFactEquipmentCreateDialogAsync(forCopy: false, source: null, pid, vid);
    }

    private async Task ShowFactEquipmentCreateDialogAsync(bool forCopy, FactDetailRow? source, int projectId, int versionId)
    {
        _peqCreateDialog.SetOwnerWindow(GetOwner()!);
        var state = SaveFactDetailStateCallback?.Invoke();
        var created = await _peqCreateDialog.ShowAsync(forCopy, source, projectId, versionId);
        if (!created) return;

        var description = forCopy && source is not null
            ? $"ProjectId={projectId}, VersionId={versionId}, copyOf={source.ProjectEquipmentId}"
            : $"ProjectId={projectId}, VersionId={versionId}";
        await LogDetailEditAsync(UserActionType.EquipmentCreate, "ProjectEquipment", 0, description);
        await LoadFactDataAsync(projectId, versionId, state);
    }

    private async Task<(int? ClassifierId, decimal Coefficient, bool? IsAssessment, bool IsKfo)?> GetCatalogVolumeMetaAsync(int catalogId)
    {
        var kfo = await _physicalVolumeKfoService.GetByIdAsync(catalogId);
        if (kfo is not null)
            return (kfo.ClassifierId, kfo.Coefficient, kfo.IsAssessment, IsKfo: true);

        var ber = await _physicalVolumeBerService.GetByIdAsync(catalogId);
        if (ber is not null)
            return (ber.ClassifierId, ber.Coefficient, ber.IsAssessment, IsKfo: false);

        return null;
    }
}

/// <summary>
/// Срез строки трудоёмкости для расчёта факта (DocumentSet + направление + часы).
/// Раньше использовался <c>IEnumerable&lt;dynamic&gt;</c>: при boxing значение <c>int?</c> с данными
/// превращалось в <c>int</c>, и вызов <c>WorkDirectionId.HasValue</c> в <see cref="ComputeFactRows"/> падал в рантайме.
/// </summary>
internal sealed record FactLaborIntensityRow(string DocSet, int? WorkDirectionId, decimal Hours);

public sealed class ForecastPerformanceItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NormUnitName { get; set; } = string.Empty;
    public decimal? FactPhysicalVolume { get; set; }
    public decimal? NormProductivity { get; set; }
    public decimal? BaseHours { get; set; }
    public decimal? OptimisticHours { get; set; }
    public decimal? PessimisticHours { get; set; }
    public bool IsTotal { get; set; }
    public int Level { get; set; }

    /// <summary>Строка для Excel/PDF — те же значения, что в колонках грида «Итого» (прогноз).</summary>
    public ForecastSummaryRowDto ToExportDto() => new(
        Code, Name, DisplayName, NormUnitName,
        FactPhysicalVolume, NormProductivity,
        BaseHours, OptimisticHours, PessimisticHours,
        IsTotal, Level);
}

public sealed class VersionCompareRow
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? ZScoreA { get; set; }
    public decimal? ZScoreB { get; set; }
    public decimal? ZDelta { get; set; }
    public string InterpretationA { get; set; } = string.Empty;
    public string InterpretationB { get; set; } = string.Empty;
}

public sealed class VersionCompareFacilityRow
{
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public decimal? FactLaborHoursA { get; set; }
    public decimal? FactLaborHoursB { get; set; }
    public decimal? HoursDelta { get; set; }
    public decimal? ZScoreA { get; set; }
    public decimal? ZScoreB { get; set; }
    public decimal? ZDelta { get; set; }
    public string InterpretationA { get; set; } = string.Empty;
    public string InterpretationB { get; set; } = string.Empty;
}

public sealed class VersionCompareDetailRow
{
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string DocumentSet { get; set; } = string.Empty;
    public string PvCode { get; set; } = string.Empty;
    public string PvName { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;

    public decimal? QuantityA { get; set; }
    public decimal? QuantityB { get; set; }
    public decimal? QuantityDelta { get; set; }

    public decimal? FactLaborHoursA { get; set; }
    public decimal? FactLaborHoursB { get; set; }
    public decimal? HoursDelta { get; set; }

    public decimal? FactProductivityA { get; set; }
    public decimal? FactProductivityB { get; set; }
    public decimal? ProductivityDelta { get; set; }

    public string IicGroupName { get; set; } = string.Empty;
    public string IicUnitName { get; set; } = string.Empty;

    public string Presence { get; set; } = string.Empty;
}

public sealed class FacilityForecastItem
{
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public decimal? BaseHours { get; set; }
    public decimal? OptimisticHours { get; set; }
    public decimal? PessimisticHours { get; set; }
    public bool IsTotal { get; set; }
}

public sealed class FactFacilityRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? FactLaborHours { get; set; }
    public decimal? ZScore { get; set; }
    public string Percentile { get; set; } = string.Empty;
    public string Interpretation { get; set; } = string.Empty;
}

public sealed class FactDetailRow
{
    public int ProjectPhysicalVolumeId { get; set; }
    public int PhysicalVolumeId { get; set; }
    public int ProjectEquipmentId { get; set; }
    public int EquipmentId { get; set; }
    public int? NormId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string BindingType { get; set; } = string.Empty;
    public string DocumentSet { get; set; } = string.Empty;
    public string CatalogId { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string WorkComment { get; set; } = string.Empty;
    public string EstimationBasis { get; set; } = string.Empty;
    public string PvCode { get; set; } = string.Empty;
    public string PvName { get; set; } = string.Empty;
    public string IicGroupName { get; set; } = string.Empty;
    public string IicUnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? FactLaborHours { get; set; }
    public decimal? FactProductivity { get; set; }
    public decimal? NormProductivity { get; set; }
    public decimal? ZScore { get; set; }
    public string Percentile { get; set; } = string.Empty;
    public string Interpretation { get; set; } = string.Empty;

    public bool IsEquipmentRow => ProjectEquipmentId > 0;

    /// <summary>Ключ для RestoreStateOnSourceChange: ФО — положительный Id, оборудование — отрицательный.</summary>
    public int DetailStateKey => IsEquipmentRow ? -ProjectEquipmentId : ProjectPhysicalVolumeId;
}

public sealed class DetailGridState
{
    public int TopRowIndex { get; set; } = 0;
    public int FocusedRowId { get; set; } = -1;
    public int FocusedVisibleOffset { get; set; } = 0;
}

public sealed class DetailForecastItem
{
    public int ProjectPhysicalVolumeId { get; set; }
    public int PhysicalVolumeId { get; set; }
    public int ProjectEquipmentId { get; set; }
    public int EquipmentId { get; set; }
    public int? ClassifierId { get; set; }
    public int? NormId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string BindingType { get; set; } = string.Empty;
    public string DocumentSet { get; set; } = string.Empty;
    public string CatalogId { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string WorkComment { get; set; } = string.Empty;
    public string EstimationBasis { get; set; } = string.Empty;
    public string PvCode { get; set; } = string.Empty;
    public string PvName { get; set; } = string.Empty;
    public string IsAssessment { get; set; } = string.Empty;
    public decimal? Coefficient { get; set; }
    public string IicGroupName { get; set; } = string.Empty;
    public string IicUnitName { get; set; } = string.Empty;
    public decimal? NormProductivity { get; set; }
    public decimal? NormStdDev { get; set; }
    public decimal Quantity { get; set; }
    public decimal? BaseHours { get; set; }
    public decimal? OptimisticHours { get; set; }
    public decimal? PessimisticHours { get; set; }
    public bool IsTotal { get; set; }

    public bool IsEquipmentRow => ProjectEquipmentId > 0;

    /// <summary>Ключ для RestoreStateOnSourceChange: ФО — положительный Id, оборудование — отрицательный.</summary>
    public int DetailStateKey => IsEquipmentRow ? -ProjectEquipmentId : ProjectPhysicalVolumeId;

    /// <summary>Строка для Excel — те же значения, что в колонках грида «Детально» (прогноз).</summary>
    public DetailForecastRowDto ToExportDto() => new(
        FacilityCode, FacilityName, BindingType, DocumentSet,
        CatalogId, Tag, WorkId, ItemName, WorkComment,
        EstimationBasis, PvCode, PvName, IsAssessment,
        Coefficient, IicGroupName, IicUnitName,
        NormProductivity, NormStdDev, Quantity,
        BaseHours, OptimisticHours, PessimisticHours);
}
