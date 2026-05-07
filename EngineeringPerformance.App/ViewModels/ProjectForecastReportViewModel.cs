using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectForecastReportViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectForecastExportService _exportService;

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

    public ProjectForecastReportViewModel(IProjectForecastExportService exportService)
    {
        _exportService = exportService;
    }

    public void SetForecastData(
        IEnumerable<ForecastPerformanceItem> items,
        string projectCode,
        string versionName)
    {
        _projectCode = projectCode;
        _versionName = versionName;

        var directions = new ObservableCollection<ForecastDirectionViewModel>();
        ForecastPerformanceItem? totalRow = null;

        foreach (var item in items)
        {
            if (item.IsTotal && item.Level == -1)
            {
                totalRow = item;
                continue;
            }
            directions.Add(new ForecastDirectionViewModel(item));
        }

        Directions = directions;

        if (totalRow is not null)
        {
            TotalBaseHours = totalRow.BaseHours ?? 0m;
            TotalOptimisticHours = totalRow.OptimisticHours ?? 0m;
            TotalPessimisticHours = totalRow.PessimisticHours ?? 0m;
        }
        else
        {
            var roots = directions.Where(d => d.Level == 0 && !d.IsTotal).ToList();
            TotalBaseHours = roots.Sum(d => d.BaseHours);
            TotalOptimisticHours = roots.Sum(d => d.OptimisticHours);
            TotalPessimisticHours = roots.Sum(d => d.PessimisticHours);
        }

        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
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
            MessageBox.Show("PDF экспорт из модального окна не поддерживается. Используйте вкладку Отчёты.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
}

public sealed class ForecastDirectionViewModel
{
    public ForecastDirectionViewModel(ForecastPerformanceItem item)
    {
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
        PhysicalVolumes = [];
    }

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
