using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectForecastReportViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectVersionAnalysisService _analysisService;
    private readonly IProjectForecastService _forecastService;
    private readonly IProjectForecastExportService _exportService;

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

    private ProjectVersionAnalysisDto? _report;

    public ProjectForecastReportViewModel(
        IProjectVersionAnalysisService analysisService,
        IProjectForecastService forecastService,
        IProjectForecastExportService exportService)
    {
        _analysisService = analysisService;
        _forecastService = forecastService;
        _exportService = exportService;
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
        if (_report is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить прогноз как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Прогноз_{_report.ProjectCode}_{_report.VersionName}.xlsx",
            DefaultExt = "xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            var forecastReport = await _forecastService.GenerateForecastAsync(ProjectId, VersionId);
            await _exportService.ExportToExcelAsync(forecastReport, dlg.FileName);
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
        if (_report is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить прогноз как PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"Прогноз_{_report.ProjectCode}_{_report.VersionName}.pdf",
            DefaultExt = "pdf",
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            var forecastReport = await _forecastService.GenerateForecastAsync(ProjectId, VersionId);
            await _exportService.ExportToPdfAsync(forecastReport, dlg.FileName);
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

    private bool CanExport() => _report is not null && !IsExporting;
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
            _report = await _analysisService.GenerateReportAsync(ProjectId, VersionId);

            ProjectNameWithVersion = string.IsNullOrWhiteSpace(_report.VersionName)
                ? _report.ProjectName
                : $"{_report.ProjectName} ({_report.VersionName})";

            var items = new ObservableCollection<ForecastDirectionViewModel>();
            foreach (var d in _report.Directions)
                items.Add(new ForecastDirectionViewModel(d));
            Directions = items;

            var roots = items.Where(d => d.ParentId == null).ToList();
            TotalBaseHours = roots.Sum(d => d.BaseHours);
            TotalOptimisticHours = roots.Sum(d => d.OptimisticHours);
            TotalPessimisticHours = roots.Sum(d => d.PessimisticHours);

            ExportToExcelCommand.NotifyCanExecuteChanged();
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
