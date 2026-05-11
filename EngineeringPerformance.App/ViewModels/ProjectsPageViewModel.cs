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
using Microsoft.EntityFrameworkCore;
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
    private readonly IDataTableService<ProjectPhysicalVolume> _ppvService;
    private readonly IProjectVersionAnalysisExportService _exportService;
    private readonly IProjectForecastExportService _forecastExportService;
    private readonly IProjectForecastService _forecastService;
    private readonly ProjectCharacteristicSelectionService _characteristicSelectionService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
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
    [ObservableProperty] private ObservableCollection<FacilityForecastItem> _factFacilityData = [];
    [ObservableProperty] private ObservableCollection<DetailForecastItem> _factDetailData = [];

    // Вкладка Сравнение версий
    [ObservableProperty] private int? _compareVersionAId;
    [ObservableProperty] private string _compareVersionAName = string.Empty;
    [ObservableProperty] private string _compareVersionALabel = string.Empty;
    [ObservableProperty] private int? _compareVersionBId;
    [ObservableProperty] private string _compareVersionBName = string.Empty;
    [ObservableProperty] private string _compareVersionBLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<VersionCompareRow> _compareRows = [];
    [ObservableProperty] private ObservableCollection<FacilityForecastItem> _compareFacilityData = [];
    [ObservableProperty] private ObservableCollection<DetailForecastItem> _compareDetailData = [];

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
        IDataTableService<ProjectPhysicalVolume> ppvService,
        IProjectVersionAnalysisExportService exportService,
        IProjectForecastExportService forecastExportService,
        ProjectCharacteristicSelectionService characteristicSelectionService,
        IDbContextFactory<AppDbContext> contextFactory,
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
        _ppvService = ppvService;
        _exportService = exportService;
        _forecastExportService = forecastExportService;
        _characteristicSelectionService = characteristicSelectionService;
        _contextFactory = contextFactory;
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);
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

        await using var context = await _contextFactory.CreateDbContextAsync();
        var project = await context.Projects.FindAsync(pid);
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

            await using var ctxCl = await _contextFactory.CreateDbContextAsync();
            var classifierMap = await ctxCl.IicWorkDirections.AsNoTracking()
                .ToDictionaryAsync(c => c.Id);

            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var pvTask = ctx.ProjectPhysicalVolumes
                .AsNoTracking()
                .Include(pv => pv.PhysicalVolume)
                .Where(pv => pv.ProjectId == projectId
                             && pv.ProjectAnalysisVersionId == versionId
                             && pv.PhysicalVolume!.IsAssessment == true
                             && pv.PhysicalVolume.ClassifierId != null)
                .ToListAsync();

            await using var ctx2 = await _contextFactory.CreateDbContextAsync();
            var pvAllTask = ctx2.ProjectPhysicalVolumes
                .AsNoTracking()
                .Include(pv => pv.PhysicalVolume)
                .Where(pv => pv.ProjectId == projectId
                             && pv.ProjectAnalysisVersionId == versionId)
                .ToListAsync();

            await using var ctxSnap = await _contextFactory.CreateDbContextAsync();
            var snapshotMapTask = ctxSnap.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Select(s => new { s.WorkDirectionId, s.NormId })
                .ToListAsync();

            await Task.WhenAll(perfTask, pvTask, pvAllTask, snapshotMapTask);
            var data = perfTask.Result;
            var physicalVolumes = pvTask.Result;
            var physicalVolumesAll = pvAllTask.Result;
            var snapshotNormIdByDirection = snapshotMapTask.Result
                .ToDictionary(s => s.WorkDirectionId, s => s.NormId);

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
                var p = row.NormProductivity.Value;
                var std = row.NormStdDev ?? 0m;
                return (
                    Math.Round(vol * p, 2),
                    std > 0 ? Math.Round(vol * Math.Max(0m, p - 0.5m * std), 2) : Math.Round(vol * p, 2),
                    std > 0 ? Math.Round(vol * (p + 0.5m * std), 2) : Math.Round(vol * p, 2)
                );
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
                    NormUnitName = row.NormUnitName ?? string.Empty,
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
                    NormUnitName = row.NormUnitName ?? string.Empty,
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

            ForecastData = new ObservableCollection<ForecastPerformanceItem>(result);

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
                    var dirId = pv.PhysicalVolume!.ClassifierId!.Value;
                    if (!normByDirection.TryGetValue(dirId, out var norm))
                        continue;

                    var vol = pv.Quantity * pv.PhysicalVolume.Coefficient;
                    var p   = norm.NormProductivity!.Value;
                    var std = norm.NormStdDev ?? 0m;

                    baseH += vol * p;
                    optH  += vol * Math.Max(0m, p - (std > 0 ? 0.5m * std : 0m));
                    pesH  += vol * (p + (std > 0 ? 0.5m * std : 0m));
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
                .ThenBy(pv => pv.PhysicalVolume?.Code ?? string.Empty);

            var detailRows = new List<DetailForecastItem>();
            foreach (var pv in allPv)
            {
                var dirId = pv.PhysicalVolume?.ClassifierId;
                PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var nd) ? nd : null;

                var isAssessment = pv.PhysicalVolume?.IsAssessment == true;
                var vol = pv.Quantity * (pv.PhysicalVolume?.Coefficient ?? 1m);
                decimal? baseH = null, optH = null, pesH = null;
                if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                {
                    var std = norm.NormStdDev ?? 0m;
                    baseH = Math.Round(vol * p, 2);
                    optH  = Math.Round(vol * Math.Max(0m, p - (std > 0 ? 0.5m * std : 0m)), 2);
                    pesH  = Math.Round(vol * (p + (std > 0 ? 0.5m * std : 0m)), 2);
                }

                detailRows.Add(new DetailForecastItem
                {
                    ProjectPhysicalVolumeId = pv.Id,
                    PhysicalVolumeId      = pv.PhysicalVolumeId,
                    NormId                = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nid) ? nid : null,
                    FacilityCode          = pv.FacilityCode ?? string.Empty,
                    FacilityName          = pv.FacilityName ?? string.Empty,
                    BindingType           = pv.BindingType ?? string.Empty,
                    DocumentSet           = pv.DocumentSet ?? string.Empty,
                    CatalogId             = pv.CatalogId ?? string.Empty,
                    Tag                   = pv.Tag ?? string.Empty,
                    WorkId                = pv.WorkId ?? string.Empty,
                    WorkName              = pv.WorkName ?? string.Empty,
                    WorkComment           = pv.WorkComment ?? string.Empty,
                    EstimationBasis       = pv.EstimationBasis ?? string.Empty,
                    PvCode                = pv.PhysicalVolume?.Code ?? string.Empty,
                    PvName                = pv.PhysicalVolume?.Name ?? string.Empty,
                    IsAssessment          = isAssessment ? "Да" : "Нет",
                    Coefficient           = pv.PhysicalVolume?.Coefficient,
                    IicGroupName          = pv.PhysicalVolume?.ClassifierId is int cid && classifierMap.TryGetValue(cid, out var cl) ? $"{cl.Code} {cl.Name}" : string.Empty,
                    IicUnitName           = norm?.NormUnitName ?? string.Empty,
                    NormProductivity      = norm?.NormProductivity,
                    NormStdDev            = norm?.NormStdDev,
                    Quantity              = pv.Quantity,
                    BaseHours             = baseH,
                    OptimisticHours       = optH,
                    PessimisticHours      = pesH,
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

            await using var ctxCl = await _contextFactory.CreateDbContextAsync();
            var classifierMap = await ctxCl.IicWorkDirections.AsNoTracking()
                .ToDictionaryAsync(c => c.Id);

            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var pvTask = ctx.ProjectPhysicalVolumes
                .AsNoTracking()
                .Include(pv => pv.PhysicalVolume)
                .Where(pv => pv.ProjectId == projectId
                             && pv.ProjectAnalysisVersionId == versionId
                             && pv.PhysicalVolume!.IsAssessment == true
                             && pv.PhysicalVolume.ClassifierId != null)
                .ToListAsync();

            await using var ctx2 = await _contextFactory.CreateDbContextAsync();
            var pvAllTask = ctx2.ProjectPhysicalVolumes
                .AsNoTracking()
                .Include(pv => pv.PhysicalVolume)
                .Where(pv => pv.ProjectId == projectId
                             && pv.ProjectAnalysisVersionId == versionId)
                .ToListAsync();

            await using var ctxSnap = await _contextFactory.CreateDbContextAsync();
            var snapshotMapTask = ctxSnap.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Select(s => new { s.WorkDirectionId, s.NormId })
                .ToListAsync();

            await Task.WhenAll(perfTask, pvTask, pvAllTask, snapshotMapTask);
            var data = perfTask.Result;
            var physicalVolumes = pvTask.Result;
            var physicalVolumesAll = pvAllTask.Result;
            var snapshotNormIdByDirection = snapshotMapTask.Result
                .ToDictionary(s => s.WorkDirectionId, s => s.NormId);

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

            // ── Вкладка «По объектам» ────────────────────────────────────────
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
                    var dirId = pv.PhysicalVolume!.ClassifierId!.Value;
                    if (!normByDirection.TryGetValue(dirId, out var norm))
                        continue;

                    var vol = pv.Quantity * pv.PhysicalVolume.Coefficient;
                    var p   = norm.NormProductivity!.Value;
                    var std = norm.NormStdDev ?? 0m;

                    baseH += vol * p;
                    optH  += vol * Math.Max(0m, p - (std > 0 ? 0.5m * std : 0m));
                    pesH  += vol * (p + (std > 0 ? 0.5m * std : 0m));
                }

                facilityResult.Add(new FacilityForecastItem
                {
                    FacilityCode     = facilityGroup.Key.Code,
                    FacilityName     = facilityGroup.Key.Name,
                    BaseHours        = baseH > 0 ? Math.Round(baseH, 2) : null,
                    OptimisticHours  = optH  > 0 ? Math.Round(optH,  2) : null,
                    PessimisticHours = pesH  > 0 ? Math.Round(pesH,  2) : null,
                });
            }

            FactFacilityData = new ObservableCollection<FacilityForecastItem>(facilityResult);

            // ── Вкладка «Детально» ──────────────────────────────────────────
            var allPv = physicalVolumesAll
                .OrderBy(pv => pv.FacilityCode ?? string.Empty)
                .ThenBy(pv => pv.FacilityName ?? string.Empty)
                .ThenBy(pv => pv.PhysicalVolume?.Code ?? string.Empty);

            var detailRows = new List<DetailForecastItem>();
            foreach (var pv in allPv)
            {
                var dirId = pv.PhysicalVolume?.ClassifierId;
                PerformanceRowDto? norm = dirId.HasValue && normByDirection.TryGetValue(dirId.Value, out var nd) ? nd : null;

                var isAssessment = pv.PhysicalVolume?.IsAssessment == true;
                var vol = pv.Quantity * (pv.PhysicalVolume?.Coefficient ?? 1m);
                decimal? baseH = null, optH = null, pesH = null;
                if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                {
                    var std = norm.NormStdDev ?? 0m;
                    baseH = Math.Round(vol * p, 2);
                    optH  = Math.Round(vol * Math.Max(0m, p - (std > 0 ? 0.5m * std : 0m)), 2);
                    pesH  = Math.Round(vol * (p + (std > 0 ? 0.5m * std : 0m)), 2);
                }

                detailRows.Add(new DetailForecastItem
                {
                    ProjectPhysicalVolumeId = pv.Id,
                    PhysicalVolumeId      = pv.PhysicalVolumeId,
                    NormId                = dirId.HasValue && snapshotNormIdByDirection.TryGetValue(dirId.Value, out var nid2) ? nid2 : null,
                    FacilityCode          = pv.FacilityCode ?? string.Empty,
                    FacilityName          = pv.FacilityName ?? string.Empty,
                    BindingType           = pv.BindingType ?? string.Empty,
                    DocumentSet           = pv.DocumentSet ?? string.Empty,
                    CatalogId             = pv.CatalogId ?? string.Empty,
                    Tag                   = pv.Tag ?? string.Empty,
                    WorkId                = pv.WorkId ?? string.Empty,
                    WorkName              = pv.WorkName ?? string.Empty,
                    WorkComment           = pv.WorkComment ?? string.Empty,
                    EstimationBasis       = pv.EstimationBasis ?? string.Empty,
                    PvCode                = pv.PhysicalVolume?.Code ?? string.Empty,
                    PvName                = pv.PhysicalVolume?.Name ?? string.Empty,
                    IsAssessment          = isAssessment ? "Да" : "Нет",
                    Coefficient           = pv.PhysicalVolume?.Coefficient,
                    IicGroupName          = pv.PhysicalVolume?.ClassifierId is int cid2 && classifierMap.TryGetValue(cid2, out var cl2) ? $"{cl2.Code} {cl2.Name}" : string.Empty,
                    IicUnitName           = norm?.NormUnitName ?? string.Empty,
                    NormProductivity      = norm?.NormProductivity,
                    NormStdDev            = norm?.NormStdDev,
                    Quantity              = pv.Quantity,
                    BaseHours             = baseH,
                    OptimisticHours       = optH,
                    PessimisticHours      = pesH,
                });
            }

            FactDetailData = new ObservableCollection<DetailForecastItem>(detailRows);
            if (savedState is not null)
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    () => RestoreFactDetailStateCallback?.Invoke(savedState),
                    System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных оценки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
        var versionId = await _versionPickerDialog.ShowPickerAsync(pid, SelectedProjectName);
        if (versionId is not int vid) return;
        CompareVersionBId = vid;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        var ver = versions.FirstOrDefault(v => v.Id == vid);
        CompareVersionBName = ver?.VersionName ?? string.Empty;
        CompareVersionBLabel = BuildVersionLabel(ver);
        await RunCompareAsync();
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
            var dataA = await Task.Run(() => _performanceService.GetPerformanceDataAsync(pid, vidA));
            var dataB = await Task.Run(() => _performanceService.GetPerformanceDataAsync(pid, vidB));

            var dictA = dataA.ToDictionary(r => r.WorkDirectionId);
            var dictB = dataB.ToDictionary(r => r.WorkDirectionId);
            var allIds = dictA.Keys.Union(dictB.Keys).ToList();

            var items = new ObservableCollection<VersionCompareRow>();
            foreach (var id in allIds)
            {
                dictA.TryGetValue(id, out var a);
                dictB.TryGetValue(id, out var b);
                var row = a ?? b!;
                var zDelta = (b?.ZScore ?? a?.ZScore) - (a?.ZScore ?? b?.ZScore);
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
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сравнения: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── Отчёты ───────────────────────────────────────────────────────────

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
                .Select(r => new ForecastSummaryRowDto(
                    r.Code, r.Name, r.DisplayName, r.NormUnitName,
                    r.FactPhysicalVolume, r.NormProductivity,
                    r.BaseHours, r.OptimisticHours, r.PessimisticHours,
                    r.IsTotal, r.Level))
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
                .Select(r => new DetailForecastRowDto(
                    r.FacilityCode, r.FacilityName, r.BindingType, r.DocumentSet,
                    r.CatalogId, r.Tag, r.WorkId, r.WorkName, r.WorkComment,
                    r.EstimationBasis, r.PvCode, r.PvName, r.IsAssessment,
                    r.Coefficient, r.IicGroupName, r.IicUnitName,
                    r.NormProductivity, r.NormStdDev, r.Quantity,
                    r.BaseHours, r.OptimisticHours, r.PessimisticHours))
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
                .Where(f => !f.IsTotal)
                .Select(f => new FacilityForecastRowDto(f.FacilityCode, f.FacilityName, f.BaseHours, f.OptimisticHours, f.PessimisticHours))
                .ToList();
            await _forecastExportService.ExportFacilityForecastToExcelAsync(rows, SelectedProjectCode, FactVersionName, dlg.FileName);
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
                .Where(f => !f.IsTotal)
                .Select(f => new FacilityForecastRowDto(f.FacilityCode, f.FacilityName, f.BaseHours, f.OptimisticHours, f.PessimisticHours))
                .ToList();
            await _forecastExportService.ExportFacilityForecastToPdfAsync(rows, SelectedProjectCode, FactVersionName, dlg.FileName);
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
                .Where(r => !r.IsTotal)
                .Select(r => new DetailForecastRowDto(
                    r.FacilityCode, r.FacilityName, r.BindingType, r.DocumentSet,
                    r.CatalogId, r.Tag, r.WorkId, r.WorkName, r.WorkComment,
                    r.EstimationBasis, r.PvCode, r.PvName, r.IsAssessment,
                    r.Coefficient, r.IicGroupName, r.IicUnitName,
                    r.NormProductivity, r.NormStdDev, r.Quantity,
                    r.BaseHours, r.OptimisticHours, r.PessimisticHours))
                .ToList();
            await _forecastExportService.ExportDetailForecastToExcelAsync(
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
        if (row is null || row.IsTotal || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

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
        if (row is null || row.IsTotal || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

        var state = SaveDetailForecastStateCallback?.Invoke();

        var vm = _serviceProvider.GetRequiredService<PhysicalVolumeQuickEditViewModel>();
        var nomenclature = $"{row.PvCode} {row.PvName}".Trim();
        vm.Prepare(row.PhysicalVolumeId, nomenclature, row.NormId.HasValue ? null : (int?)null,
            row.Coefficient ?? 1m, row.IsAssessment == "Да" ? true : row.IsAssessment == "Нет" ? false : (bool?)null);

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var pv = await ctx.PhysicalVolumes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == row.PhysicalVolumeId);
        if (pv is not null)
            vm.Prepare(row.PhysicalVolumeId, nomenclature, pv.ClassifierId, pv.Coefficient, pv.IsAssessment);

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
        if (row is null || row.IsTotal || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;

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
        if (row is null || row.IsTotal || SelectedProjectId is not int pid || PlanVersionId is not int vid) return;
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
}

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

public sealed class FacilityForecastItem
{
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public decimal? BaseHours { get; set; }
    public decimal? OptimisticHours { get; set; }
    public decimal? PessimisticHours { get; set; }
    public bool IsTotal { get; set; }
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
    public int? NormId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string BindingType { get; set; } = string.Empty;
    public string DocumentSet { get; set; } = string.Empty;
    public string CatalogId { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;
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
}
