using System.Collections.ObjectModel;
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
    private readonly IProjectVersionAnalysisExportService _exportService;
    private readonly IProjectForecastExportService _forecastExportService;
    private readonly IProjectForecastService _forecastService;
    private readonly ProjectCharacteristicSelectionService _characteristicSelectionService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;

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

    // Вкладка Оценка факта — выбранная версия (факт)
    [ObservableProperty] private int? _factVersionId;
    [ObservableProperty] private string _factVersionName = string.Empty;
    [ObservableProperty] private string _factVersionLabel = string.Empty;
    [ObservableProperty] private ObservableCollection<PerformanceItem> _factPerformanceData = [];

    // Вкладка Сравнение версий
    [ObservableProperty] private int? _compareVersionAId;
    [ObservableProperty] private string _compareVersionAName = string.Empty;
    [ObservableProperty] private int? _compareVersionBId;
    [ObservableProperty] private string _compareVersionBName = string.Empty;
    [ObservableProperty] private ObservableCollection<VersionCompareRow> _compareRows = [];

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
        IProjectVersionAnalysisExportService exportService,
        IProjectForecastExportService forecastExportService,
        ProjectCharacteristicSelectionService characteristicSelectionService,
        IDbContextFactory<AppDbContext> contextFactory,
        IServiceProvider serviceProvider)
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
        _exportService = exportService;
        _forecastExportService = forecastExportService;
        _characteristicSelectionService = characteristicSelectionService;
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
    }

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

        FactVersionId = null;
        FactVersionName = string.Empty;
        FactVersionLabel = string.Empty;
        FactPerformanceData = [];

        CompareVersionAId = null;
        CompareVersionAName = string.Empty;
        CompareVersionBId = null;
        CompareVersionBName = string.Empty;
        CompareRows = [];

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

    private async Task LoadForecastDataAsync(int projectId, int versionId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var data = await Task.Run(() => _performanceService.GetPerformanceDataAsync(projectId, versionId));

            // Строим плоский список с итоговыми строками для узлов, имеющих потомков.
            // Алгоритм: рекурсивный обход дерева (pre-order узел + дети + post-order итог).
            var rowById = data
                .Where(r => r.WorkDirectionId >= 0)
                .ToDictionary(r => r.WorkDirectionId);
            var childrenByParent = data
                .Where(r => r.WorkDirectionId >= 0 && r.ParentId.HasValue)
                .ToLookup(r => r.ParentId!.Value);
            var roots = data
                .Where(r => r.WorkDirectionId >= 0 && !r.ParentId.HasValue)
                .ToList();

            var result = new List<ForecastPerformanceItem>();

            (decimal? baseH, decimal? optH, decimal? pesH) CalcHours(PerformanceRowDto row)
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

            // Возвращает агрегат поддерева (baseH, optH, pesH) включая собственные данные узла.
            (decimal baseH, decimal optH, decimal pesH) BuildSubtree(PerformanceRowDto row, int level)
            {
                var (ownBase, ownOpt, ownPes) = CalcHours(row);
                var children = childrenByParent[row.WorkDirectionId].ToList();
                bool hasChildren = children.Count > 0;

                var indent = new string(' ', level * 4);
                // Добавляем строку самого узла
                result.Add(new ForecastPerformanceItem
                {
                    Id = row.WorkDirectionId,
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

                // Рекурсивно обрабатываем детей
                decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
                foreach (var child in children)
                {
                    var (cb, co, cp) = BuildSubtree(child, level + 1);
                    totalBase += cb;
                    totalOpt += co;
                    totalPes += cp;
                }

                // Итог = собственные данные узла + агрегат всех потомков
                var aggBase = (ownBase ?? 0m) + totalBase;
                var aggOpt = (ownOpt ?? 0m) + totalOpt;
                var aggPes = (ownPes ?? 0m) + totalPes;

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

            // Строка «(пусто)» из PerformanceService — нераспределённые ФО
            var unallocated = data.FirstOrDefault(r => r.WorkDirectionId < 0);
            if (unallocated is not null)
            {
                var (ub, uo, up) = CalcHours(unallocated);
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

            ForecastData = new ObservableCollection<ForecastPerformanceItem>(result);
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
        vm.OwnerWindow = window;
        window.DataContext = vm;
        window.Owner = owner;
        _ = vm.LoadAsync();
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
            label += $" — {ver.Description}";
        return label;
    }

    private async Task LoadFactDataAsync(int projectId, int versionId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var data = await Task.Run(() => _performanceService.GetPerformanceDataAsync(projectId, versionId));
            var items = new ObservableCollection<PerformanceItem>();
            foreach (var row in data)
            {
                items.Add(new PerformanceItem
                {
                    Id = row.WorkDirectionId,
                    ParentId = row.ParentId,
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
                    Interpretation = row.Interpretation?.ToRussian() ?? string.Empty,
                });
            }
            FactPerformanceData = items;
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
        var result = await _projectSelectionDialog.ShowSelectionDialogAsync(AnalysisDirection.Fact);
        if (result is not var (_, versionId)) return;
        CompareVersionAId = versionId;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        CompareVersionAName = versions.FirstOrDefault(v => v.Id == versionId)?.VersionName ?? string.Empty;
        await RunCompareAsync();
    }

    [RelayCommand]
    private async Task SelectCompareVersionBAsync()
    {
        if (SelectedProjectId is not int pid) return;
        SetDialogOwners();
        var result = await _projectSelectionDialog.ShowSelectionDialogAsync(AnalysisDirection.Fact);
        if (result is not var (_, versionId)) return;
        CompareVersionBId = versionId;
        var versions = await _versionQueryService.GetByProjectAsync(pid, includeArchived: false);
        CompareVersionBName = versions.FirstOrDefault(v => v.Id == versionId)?.VersionName ?? string.Empty;
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
                    InterpretationA = a?.Interpretation?.ToRussian() ?? "—",
                    InterpretationB = b?.Interpretation?.ToRussian() ?? "—",
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
            var analysisSvc = _serviceProvider.GetRequiredService<Core.Services.IProjectVersionAnalysisService>();
            var report = await analysisSvc.GenerateReportAsync(pid, vid);
            await _forecastExportService.ExportToExcelAsync(report, dlg.FileName);
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
}

public sealed class ForecastPerformanceItem
{
    public int Id { get; set; }
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
