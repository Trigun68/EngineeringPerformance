using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class AssessmentFormViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectAnalysisService _analysisService;
    private readonly IProjectSelectionDialogService _projectSelectionDialogService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    private string _selectedProjectName = string.Empty;

    [ObservableProperty]
    private string _selectedVersionName = string.Empty;

    [ObservableProperty]
    private int? _selectedProjectId;

    [ObservableProperty]
    private int? _selectedVersionId;

    [ObservableProperty]
    private ProjectAnalysisReportDto? _report;

    /// <summary>
    /// Отображаемое имя проекта в блоке «Информация о проекте»: «Название (Версия)».
    /// Скобки с версией добавляются только если версия известна.
    /// </summary>
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

    partial void OnReportChanged(ProjectAnalysisReportDto? value) => OnPropertyChanged(nameof(ProjectNameWithVersion));

    [ObservableProperty]
    private ObservableCollection<DisciplineAnalysisDto> _disciplines = [];

    [ObservableProperty]
    private ObservableCollection<AccuracyCharacteristicDto> _accuracyCharacteristics = [];

    [ObservableProperty]
    private ObservableCollection<string> _recommendations = [];

    [ObservableProperty]
    private ObservableCollection<string> _keyDisciplines = [];

    [ObservableProperty]
    private string _permBasedRecommendation = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public Window? OwnerWindow { get; set; }

    public AssessmentFormViewModel(
        IProjectAnalysisService analysisService,
        IProjectSelectionDialogService projectSelectionDialogService)
    {
        _analysisService = analysisService;
        _projectSelectionDialogService = projectSelectionDialogService;
    }

    public async Task LoadAsync()
    {
        if (SelectedProjectId is null || SelectedVersionId is null)
            return;

        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task SelectProjectAsync()
    {
        if (OwnerWindow is not null)
        {
            _projectSelectionDialogService.SetOwnerWindow(OwnerWindow);
        }

        var result = await _projectSelectionDialogService.ShowSelectionDialogAsync();
        if (result is not var (projectId, versionId))
            return;

        SelectedProjectId = projectId;
        SelectedVersionId = versionId;

        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedProjectId is null || SelectedVersionId is null)
            return;

        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (Report is null)
            return;

        await Task.CompletedTask;
    }

    private async Task LoadReportAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (SelectedProjectId is not int projectId || SelectedVersionId is not int versionId)
                return;

            var report = await _analysisService.GenerateReportAsync(projectId, versionId);

            if (token.IsCancellationRequested)
                return;

            Report = report;
            SelectedProjectName = report.ProjectName;
            SelectedVersionName = report.VersionName;

            Disciplines.Clear();
            foreach (var discipline in report.Disciplines)
            {
                Disciplines.Add(discipline);
            }

            AccuracyCharacteristics.Clear();
            foreach (var characteristic in report.AccuracyCharacteristics)
            {
                AccuracyCharacteristics.Add(characteristic);
            }

            Recommendations.Clear();
            foreach (var recommendation in report.Recommendations)
            {
                Recommendations.Add(recommendation);
            }

            KeyDisciplines.Clear();
            foreach (var keyDiscipline in report.KeyDisciplines)
            {
                KeyDisciplines.Add(keyDiscipline);
            }

            PermBasedRecommendation = GeneratePermRecommendation(report.PermIndicator);
        }
        catch (OperationCanceledException)
        {
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

    private static string GeneratePermRecommendation(decimal? perm) => perm switch
    {
        null => "Нет данных для анализа",
        < -1.0m => "Производительность отличная. Продолжайте в том же духе!",
        >= -1.0m and < -0.5m => "Производительность выше среднего. Поддерживайте текущий уровень.",
        >= -0.5m and <= 0.5m => "Производительность около среднего. Есть потенциал для улучшения.",
        > 0.5m and <= 1.0m => "Производительность ниже среднего. Рекомендуется оптимизация.",
        > 1.0m => "Производительность значительно ниже среднего. Требуются срочные меры.",
    };
}
