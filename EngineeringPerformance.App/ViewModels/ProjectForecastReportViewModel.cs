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
    private readonly IProjectForecastService _forecastService;
    private readonly IProjectForecastExportService _exportService;

    public int ProjectId { get; set; }
    public int VersionId { get; set; }
    public Window? OwnerWindow { get; set; }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _projectNameWithVersion = string.Empty;
    [ObservableProperty] private ObservableCollection<ForecastRowViewModel> _rows = [];
    [ObservableProperty] private decimal _totalBaseHours;
    [ObservableProperty] private decimal _totalOptimisticHours;
    [ObservableProperty] private decimal _totalPessimisticHours;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    private ProjectForecastReportDto? _report;

    public ProjectForecastReportViewModel(
        IProjectForecastService forecastService,
        IProjectForecastExportService exportService)
    {
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
            await _exportService.ExportToExcelAsync(_report, dlg.FileName);
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

    private bool CanExport() => _report is not null && !IsExporting;

    partial void OnIsExportingChanged(bool value) => ExportToExcelCommand.NotifyCanExecuteChanged();

    private async Task LoadReportAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            _report = await _forecastService.GenerateForecastAsync(ProjectId, VersionId);

            ProjectNameWithVersion = string.IsNullOrWhiteSpace(_report.VersionName)
                ? _report.ProjectName
                : $"{_report.ProjectName} ({_report.VersionName})";

            Rows.Clear();
            foreach (var v in _report.PlannedVolumes)
                Rows.Add(new ForecastRowViewModel(v));

            // Суммы только по корневым узлам (без ParentId)
            var roots = _report.PlannedVolumes.Where(r => r.ParentId == null).ToList();
            TotalBaseHours = roots.Sum(r => r.BaseHours);
            TotalOptimisticHours = roots.Sum(r => r.OptimisticHours);
            TotalPessimisticHours = roots.Sum(r => r.PessimisticHours);

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

public sealed class ForecastRowViewModel
{
    private readonly ForecastIicVolumeRowDto _dto;

    public ForecastRowViewModel(ForecastIicVolumeRowDto dto)
    {
        _dto = dto;
    }

    public int Id => _dto.WorkDirectionId;
    public int? ParentId => _dto.ParentId;
    public string Name => _dto.Name;
    public string UnitName => _dto.UnitName ?? string.Empty;
    public decimal? NormProductivity => _dto.NormProductivity;
    public decimal PlannedVolume => _dto.PlannedVolume;
    public decimal BaseHours => _dto.BaseHours;
    public decimal OptimisticHours => _dto.OptimisticHours;
    public decimal PessimisticHours => _dto.PessimisticHours;
    public string Range => _dto.Range;

    /// <summary>Отступ карточки по уровню иерархии.</summary>
    public double IndentMargin
    {
        get
        {
            // Вычисляем через HierarchyPath: количество разделителей = уровень
            if (string.IsNullOrEmpty(_dto.HierarchyPath)) return 0;
            var level = _dto.HierarchyPath.Count(c => c == '/');
            return Math.Max(0, level) * 20d;
        }
    }
}
