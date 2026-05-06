using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectVersionAnalysisViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectVersionAnalysisService _service;
    private readonly IProjectVersionAnalysisExportService _exportService;

    [ObservableProperty]
    private ProjectVersionAnalysisDto? _report;

    [ObservableProperty]
    private ObservableCollection<DirectionViewModel> _directions = [];

    [ObservableProperty]
    private ObservableCollection<ProjectVersionLaborIntensityDto> _laborIntensities = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public int ProjectId { get; set; }

    public int VersionId { get; set; }

    public Window? OwnerWindow { get; set; }

    public string ProjectNameWithVersion
    {
        get
        {
            var name = Report?.ProjectName ?? string.Empty;
            var version = Report?.VersionName;
            if (string.IsNullOrWhiteSpace(version))
            {
                return name;
            }
            return string.IsNullOrWhiteSpace(name) ? $"({version})" : $"{name} ({version})";
        }
    }

    [ObservableProperty]
    private bool _isExporting;

    public ProjectVersionAnalysisViewModel(
        IProjectVersionAnalysisService service,
        IProjectVersionAnalysisExportService exportService)
    {
        _service = service;
        _exportService = exportService;
    }

    public async Task LoadAsync()
    {
        if (ProjectId <= 0 || VersionId <= 0)
            return;

        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadReportAsync();

    [RelayCommand]
    private void Close()
    {
        OwnerWindow?.Close();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToExcelAsync()
    {
        if (Report is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = BuildFileName("xlsx")
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            await _exportService.ExportToExcelAsync(Report, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToPdfAsync()
    {
        if (Report is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = BuildFileName("pdf")
        };
        if (dlg.ShowDialog() != true) return;

        IsExporting = true;
        try
        {
            await _exportService.ExportToPdfAsync(Report, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExport() => Report is not null && !IsExporting;

    partial void OnReportChanged(ProjectVersionAnalysisDto? value)
    {
        OnPropertyChanged(nameof(ProjectNameWithVersion));
        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExportingChanged(bool value)
    {
        ExportToExcelCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }

    private string BuildFileName(string ext)
    {
        var name = Report?.ProjectName ?? "отчёт";
        var version = Report?.VersionName ?? string.Empty;
        var safe = string.Concat((name + "_" + version).Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return $"{safe}.{ext}";
    }

    public decimal? TotalFactPhysicalVolume => Directions
        .Where(d => d.ParentId == null)
        .Sum(d => d.FactPhysicalVolume ?? 0m) is var s && s > 0 ? s : null;

    public decimal? TotalFactLaborHours => Directions
        .Where(d => d.ParentId == null)
        .Sum(d => d.FactLaborHours ?? 0m) is var s && s > 0 ? s : null;

    private async Task LoadReportAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var report = await _service.GenerateReportAsync(ProjectId, VersionId);
            Report = report;

            Directions.Clear();
            foreach (var d in report.Directions)
            {
                Directions.Add(new DirectionViewModel(d));
            }

            OnPropertyChanged(nameof(TotalFactPhysicalVolume));
            OnPropertyChanged(nameof(TotalFactLaborHours));

            LaborIntensities.Clear();
            foreach (var li in report.LaborIntensities)
            {
                LaborIntensities.Add(li);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки отчёта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Декоратор над <see cref="ProjectVersionDirectionDto"/> с готовыми UI-полями
/// (отступ по уровню, человекочитаемая интерпретация, флаг наличия физ. объёмов).
/// </summary>
public sealed class DirectionViewModel
{
    public DirectionViewModel(ProjectVersionDirectionDto dto)
    {
        Dto = dto;
    }

    public ProjectVersionDirectionDto Dto { get; }

    public int Id => Dto.Id;
    public int? ParentId => Dto.ParentId;
    public int Level => Dto.Level;
    public string Code => Dto.Code;
    public string Name => Dto.Name;
    public string? UnitName => Dto.UnitName;

    public decimal? FactPhysicalVolume => Dto.FactPhysicalVolume;
    public decimal? FactLaborHours => Dto.FactLaborHours;
    public decimal? FactProductivity => Dto.FactProductivity;
    public decimal? FactEstimate => Dto.FactEstimate;
    public decimal? NormProductivity => Dto.NormProductivity;
    public decimal? NormEstimate => Dto.NormEstimate;
    public decimal? NormStdDev => Dto.NormStdDev;
    public decimal? ZScore => Dto.ZScore;

    public string InterpretationText => Dto.Interpretation?.ToRussian() ?? "—";

    public IReadOnlyList<ProjectVersionPhysicalVolumeDto> PhysicalVolumes => Dto.PhysicalVolumes;
    public IReadOnlyList<ProjectVersionDirectionLaborDto> DirectionLaborIntensities => Dto.DirectionLaborIntensities;

    public bool HasPhysicalVolumes => Dto.PhysicalVolumes.Count > 0;
    public bool HasDirectionLabor => Dto.DirectionLaborIntensities.Count > 0;

    /// <summary>ФО нет, но трудоёмкость есть — подсветить ячейку «Факт ФО».</summary>
    public bool HighlightMissingPhysicalVolume => !Dto.FactPhysicalVolume.HasValue && Dto.FactLaborHours.HasValue;

    /// <summary>Трудоёмкость нет, но ФО есть — подсветить ячейку «Факт труд., ч».</summary>
    public bool HighlightMissingLaborHours => !Dto.FactLaborHours.HasValue && Dto.FactPhysicalVolume.HasValue;

    /// <summary>Левый отступ карточки в иерархии (px). Корень = 0.</summary>
    public double IndentMargin => Math.Max(0, Level) * 24d;
}
