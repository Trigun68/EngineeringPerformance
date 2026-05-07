using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectForecastReportViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectForecastExportService _exportService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public int ProjectId { get; set; }
    public int VersionId { get; set; }
    public Window? OwnerWindow { get; set; }

    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _projectNameWithVersion = string.Empty;
    [ObservableProperty] private ObservableCollection<ForecastDirectionViewModel> _directions = [];
    [ObservableProperty] private decimal _totalBaseHours;
    [ObservableProperty] private decimal _totalOptimisticHours;
    [ObservableProperty] private decimal _totalPessimisticHours;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    private string _projectCode = string.Empty;
    private string _versionName = string.Empty;

    public ProjectForecastReportViewModel(
        IProjectForecastExportService exportService,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _exportService = exportService;
        _contextFactory = contextFactory;
    }

    public void SetForecastData(
        IEnumerable<ForecastPerformanceItem> items,
        string projectCode,
        string versionName)
    {
        _projectCode = projectCode;
        _versionName = versionName;
        _pendingItems = items.ToList();

        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();

        _ = LoadAndBuildAsync();
    }

    private List<ForecastPerformanceItem> _pendingItems = [];

    private async Task LoadAndBuildAsync()
    {
        IReadOnlyDictionary<int, List<ProjectVersionPhysicalVolumeDto>> pvByDirection =
            new Dictionary<int, List<ProjectVersionPhysicalVolumeDto>>();

        if (ProjectId > 0 && VersionId > 0)
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var pvList = await ctx.ProjectPhysicalVolumes
                .AsNoTracking()
                .Include(pv => pv.PhysicalVolume).ThenInclude(p => p!.Unit)
                .Where(pv => pv.ProjectId == ProjectId
                             && pv.ProjectAnalysisVersionId == VersionId
                             && pv.Quantity > 0
                             && pv.PhysicalVolume!.IsAssessment == true
                             && pv.PhysicalVolume.ClassifierId != null)
                .ToListAsync();

            pvByDirection = pvList
                .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
                .ToDictionary(g => g.Key, g => g
                    .Select(pv =>
                    {
                        var coeff = pv.PhysicalVolume?.Coefficient ?? 1m;
                        return new ProjectVersionPhysicalVolumeDto(
                            Code: pv.PhysicalVolume?.Code ?? string.Empty,
                            Name: pv.PhysicalVolume?.Name ?? string.Empty,
                            UnitName: pv.PhysicalVolume?.Unit?.Name,
                            Quantity: pv.Quantity,
                            Coefficient: coeff,
                            Total: pv.Quantity * coeff);
                    })
                    .OrderBy(p => p.Code)
                    .ToList());
        }

        var directions = new ObservableCollection<ForecastDirectionViewModel>();
        ForecastPerformanceItem? totalRow = null;

        foreach (var item in _pendingItems)
        {
            if (item.IsTotal && item.Level == -1) { totalRow = item; continue; }
            pvByDirection.TryGetValue(item.Id, out var pvs);
            directions.Add(new ForecastDirectionViewModel(item, pvs ?? []));
        }

        Directions = directions;

        if (totalRow is not null)
        {
            TotalBaseHours       = totalRow.BaseHours       ?? 0m;
            TotalOptimisticHours = totalRow.OptimisticHours ?? 0m;
            TotalPessimisticHours= totalRow.PessimisticHours?? 0m;
        }
        else
        {
            var roots = directions.Where(d => d.Level == 0 && !d.IsTotal).ToList();
            TotalBaseHours       = roots.Sum(d => d.BaseHours);
            TotalOptimisticHours = roots.Sum(d => d.OptimisticHours);
            TotalPessimisticHours= roots.Sum(d => d.PessimisticHours);
        }
    }

    public Task LoadAsync() => Task.CompletedTask;

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
            var rows = BuildExportRows();
            await _exportService.ExportForecastSummaryToExcelAsync(rows, _projectCode, _versionName, dlg.FileName);
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
            var rows = BuildExportRows();
            await _exportService.ExportForecastSummaryToPdfAsync(rows, _projectCode, _versionName, dlg.FileName);
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

    private List<ForecastSummaryRowDto> BuildExportRows()
    {
        var rows = Directions
            .Select(d => new ForecastSummaryRowDto(
                d.Code, d.Name, d.DisplayName, d.UnitName ?? string.Empty,
                d.FactPhysicalVolume, d.NormProductivity,
                d.BaseHours, d.OptimisticHours, d.PessimisticHours,
                d.IsTotal, d.Level))
            .ToList();
        if (TotalBaseHours > 0 || TotalOptimisticHours > 0 || TotalPessimisticHours > 0)
        {
            rows.Add(new ForecastSummaryRowDto(
                string.Empty, "ИТОГО", "ИТОГО", string.Empty,
                null, null,
                TotalBaseHours, TotalOptimisticHours, TotalPessimisticHours,
                true, -1));
        }
        return rows;
    }

    private bool CanExport() => !string.IsNullOrEmpty(_versionName) && !IsExporting;
    partial void OnIsExportingChanged(bool value)
    {
        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }
}

public sealed class ForecastDirectionViewModel
{
    public ForecastDirectionViewModel(ForecastPerformanceItem item, IReadOnlyList<ProjectVersionPhysicalVolumeDto> physicalVolumes)
    {
        Id = item.Id;
        Code = item.Code;
        Name = item.Name;
        DisplayName = item.DisplayName;
        UnitName = string.IsNullOrEmpty(item.NormUnitName) ? null : item.NormUnitName;
        FactPhysicalVolume = item.FactPhysicalVolume;
        NormProductivity = item.NormProductivity;
        BaseHours = item.BaseHours ?? 0m;
        OptimisticHours = item.OptimisticHours ?? 0m;
        PessimisticHours = item.PessimisticHours ?? 0m;
        IsTotal = item.IsTotal;
        Level = item.Level;
        IndentMargin = Math.Max(0, item.Level) * 24d;
        PhysicalVolumes = physicalVolumes;
    }

    public int Id { get; }
    public string Code { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string? UnitName { get; }
    public decimal? FactPhysicalVolume { get; }
    public decimal? NormProductivity { get; }
    public decimal BaseHours { get; }
    public decimal OptimisticHours { get; }
    public decimal PessimisticHours { get; }
    public bool IsTotal { get; }
    public int Level { get; }
    public IReadOnlyList<ProjectVersionPhysicalVolumeDto> PhysicalVolumes { get; }
    public double IndentMargin { get; }
}
