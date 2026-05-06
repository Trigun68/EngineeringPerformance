using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectForecastReportViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IPerformanceService _performanceService;
    private readonly IProjectVersionAnalysisService _analysisService;
    private readonly IProjectForecastExportService _exportService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public int ProjectId { get; set; }
    public int VersionId { get; set; }
    public Window? OwnerWindow { get; set; }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _projectNameWithVersion = string.Empty;
    [ObservableProperty] private ObservableCollection<ForecastDirectionViewModel> _directions = [];
    [ObservableProperty] private decimal _totalBaseHours;
    [ObservableProperty] private decimal _totalOptimisticHours;
    [ObservableProperty] private decimal _totalPessimisticHours;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    private string _projectName = string.Empty;
    private string _projectCode = string.Empty;
    private string _versionName = string.Empty;

    public ProjectForecastReportViewModel(
        IPerformanceService performanceService,
        IProjectVersionAnalysisService analysisService,
        IProjectForecastExportService exportService,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _performanceService = performanceService;
        _analysisService = analysisService;
        _exportService = exportService;
        _contextFactory = contextFactory;
    }

    public async Task LoadAsync()
    {
        if (ProjectId <= 0 || VersionId <= 0) return;
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadReportAsync();

    [RelayCommand]
    private void Close() => OwnerWindow?.Close();

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToExcelAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить прогноз как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Прогноз_{_projectCode}_{_versionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            var report = await _analysisService.GenerateReportAsync(ProjectId, VersionId);
            await _exportService.ExportToExcelAsync(report, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
            ExportToExcelCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToPdfAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить прогноз как PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"Прогноз_{_projectCode}_{_versionName}.pdf",
            DefaultExt = "pdf",
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            var report = await _analysisService.GenerateReportAsync(ProjectId, VersionId);
            await _exportService.ExportToPdfAsync(report, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
            ExportToPdfCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExport() => !string.IsNullOrEmpty(_versionName) && !IsExporting;
    partial void OnIsExportingChanged(bool value)
    {
        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadReportAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Параллельно: лёгкий PerformanceService (кеш WorkDirections) + метаданные версии + физ. объёмы
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            var versionTask = ctx.ProjectAnalysisVersions
                .AsNoTracking()
                .Include(v => v.Project)
                .FirstOrDefaultAsync(v => v.Id == VersionId && v.ProjectId == ProjectId);

            var perfTask = _performanceService.GetPerformanceDataAsync(ProjectId, VersionId);

            await using var ctx2 = await _contextFactory.CreateDbContextAsync();
            var pvTask = ctx2.ProjectPhysicalVolumes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(pv => pv.PhysicalVolume).ThenInclude(p => p!.Unit)
                .Where(pv => pv.ProjectId == ProjectId && pv.ProjectAnalysisVersionId == VersionId
                             && pv.Quantity > 0)
                .ToListAsync();

            await Task.WhenAll(versionTask, perfTask, pvTask);

            var version = versionTask.Result;
            var performanceData = perfTask.Result;
            var physicalVolumes = pvTask.Result;

            if (version is not null)
            {
                _projectName = version.Project.Name;
                _projectCode = version.Project.Code ?? string.Empty;
                _versionName = version.VersionName;
                ProjectNameWithVersion = string.IsNullOrWhiteSpace(_versionName)
                    ? _projectName
                    : $"{_projectName} ({_versionName})";
            }

            // Группировка физ. объёмов по направлению
            var pvByDirection = physicalVolumes
                .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true && pv.PhysicalVolume.IsAssessment == true)
                .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = new ObservableCollection<ForecastDirectionViewModel>();
            foreach (var row in performanceData.Where(r => r.WorkDirectionId >= 0))
            {
                pvByDirection.TryGetValue(row.WorkDirectionId, out var pvList);
                var physVols = pvList?.Select(pv =>
                {
                    var coeff = pv.PhysicalVolume?.Coefficient ?? 1m;
                    return new ProjectVersionPhysicalVolumeDto(
                        Code: pv.PhysicalVolume?.Code ?? string.Empty,
                        Name: pv.PhysicalVolume?.Name ?? string.Empty,
                        UnitName: pv.PhysicalVolume?.Unit?.Name,
                        Quantity: pv.Quantity,
                        Coefficient: coeff,
                        Total: pv.Quantity * coeff);
                }).OrderBy(p => p.Code).ToList()
                ?? new List<ProjectVersionPhysicalVolumeDto>();

                var dto = new ProjectVersionDirectionDto(
                    Id: row.WorkDirectionId,
                    ParentId: row.ParentId,
                    Level: row.Level,
                    Code: row.WorkDirectionCode,
                    Name: row.WorkDirectionName,
                    UnitName: row.NormUnitName,
                    FactPhysicalVolume: row.FactPhysicalVolume,
                    FactLaborHours: row.FactLaborHours,
                    FactProductivity: row.FactProductivity,
                    FactEstimate: row.FactEstimate,
                    NormProductivity: row.NormProductivity,
                    NormEstimate: row.NormEstimate,
                    NormStdDev: row.NormStdDev,
                    ZScore: row.ZScore,
                    Interpretation: row.Interpretation,
                    PhysicalVolumes: physVols,
                    DirectionLaborIntensities: Array.Empty<ProjectVersionDirectionLaborDto>());

                items.Add(new ForecastDirectionViewModel(dto));
            }

            Directions = items;

            var roots = items.Where(d => d.ParentId == null).ToList();
            TotalBaseHours = roots.Sum(d => d.BaseHours);
            TotalOptimisticHours = roots.Sum(d => d.OptimisticHours);
            TotalPessimisticHours = roots.Sum(d => d.PessimisticHours);

            ExportToExcelCommand.NotifyCanExecuteChanged();
            ExportToPdfCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки прогноза: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public sealed class ForecastDirectionViewModel
{
    public ForecastDirectionViewModel(ProjectVersionDirectionDto dto)
    {
        Id = dto.Id;
        ParentId = dto.ParentId;
        Code = dto.Code;
        Name = dto.Name;
        UnitName = dto.UnitName;
        FactPhysicalVolume = dto.FactPhysicalVolume;
        NormProductivity = dto.NormProductivity;
        NormStdDev = dto.NormStdDev;
        PhysicalVolumes = dto.PhysicalVolumes;
        IndentMargin = Math.Max(0, dto.Level) * 24d;

        if (dto.FactPhysicalVolume.HasValue && dto.NormProductivity.HasValue && dto.NormProductivity.Value > 0)
        {
            BaseHours = Math.Round(dto.FactPhysicalVolume.Value * dto.NormProductivity.Value, 2);
            if (dto.NormStdDev.HasValue && dto.NormStdDev.Value > 0)
            {
                OptimisticHours = Math.Round(dto.FactPhysicalVolume.Value * Math.Max(0m, dto.NormProductivity.Value - 0.5m * dto.NormStdDev.Value), 2);
                PessimisticHours = Math.Round(dto.FactPhysicalVolume.Value * (dto.NormProductivity.Value + 0.5m * dto.NormStdDev.Value), 2);
            }
            else
            {
                OptimisticHours = BaseHours;
                PessimisticHours = BaseHours;
            }
        }
    }

    public int Id { get; }
    public int? ParentId { get; }
    public string Code { get; }
    public string Name { get; }
    public string? UnitName { get; }
    public decimal? FactPhysicalVolume { get; }
    public decimal? NormProductivity { get; }
    public decimal? NormStdDev { get; }
    public decimal BaseHours { get; }
    public decimal OptimisticHours { get; }
    public decimal PessimisticHours { get; }
    public IReadOnlyList<ProjectVersionPhysicalVolumeDto> PhysicalVolumes { get; }
    public double IndentMargin { get; }
}
