using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Models;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PlanningFormViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IProjectForecastService _forecastService;
    private readonly IProjectSelectionDialogService _projectSelectionDialogService;
    private readonly IForecastPlannedVolumePersistenceService _plannedVolumePersistence;
    private CancellationTokenSource? _loadCts;
    private int _previewGeneration;

    [ObservableProperty]
    private string _selectedProjectName = string.Empty;

    [ObservableProperty]
    private string _selectedVersionName = string.Empty;

    [ObservableProperty]
    private int? _selectedProjectId;

    [ObservableProperty]
    private int? _selectedVersionId;

    [ObservableProperty]
    private ProjectForecastReportDto? _report;

    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, string>> _projectCharacteristics = [];

    [ObservableProperty]
    private ObservableCollection<ForecastIicVolumeRowModel> _plannedVolumes = [];

    [ObservableProperty]
    private ObservableCollection<ForecastScenarioDto> _scenarios = [];

    [ObservableProperty]
    private ObservableCollection<ScenarioInterpretationDto> _interpretations = [];

    [ObservableProperty]
    private ObservableCollection<TopDisciplineDto> _top3Disciplines = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public Window? OwnerWindow { get; set; }

    public PlanningFormViewModel(
        IProjectForecastService forecastService,
        IProjectSelectionDialogService projectSelectionDialogService,
        IForecastPlannedVolumePersistenceService plannedVolumePersistence)
    {
        _forecastService = forecastService;
        _projectSelectionDialogService = projectSelectionDialogService;
        _plannedVolumePersistence = plannedVolumePersistence;
    }

    public bool HasUnsavedPlannedVolumes => PlannedVolumes.Any(r => r.IsPlannedVolumeDirty);

    public async Task LoadAsync()
    {
        if (SelectedProjectId is null || SelectedVersionId is null)
            return;

        await LoadForecastAsync();
    }

    [RelayCommand]
    private async Task SelectProjectAsync()
    {
        if (OwnerWindow is not null)
        {
            _projectSelectionDialogService.SetOwnerWindow(OwnerWindow);
        }

        var result = await _projectSelectionDialogService.ShowSelectionDialogAsync(AnalysisDirection.Plan);
        if (result is not var (projectId, versionId))
            return;

        SelectedProjectId = projectId;
        SelectedVersionId = versionId;

        await LoadForecastAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedProjectId is null || SelectedVersionId is null)
            return;

        await LoadForecastAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSavePlannedVolumes))]
    private async Task SavePlannedVolumesAsync()
    {
        if (SelectedProjectId is not int projectId || SelectedVersionId is not int versionId)
            return;

        var dirtyLeaves = PlannedVolumes.Where(r => r.IsPlannedVolumeDirty).ToList();
        if (dirtyLeaves.Count == 0)
            return;

        ErrorMessage = null;
        IsLoading = true;
        try
        {
            foreach (var row in dirtyLeaves)
            {
                await _plannedVolumePersistence.SavePlannedVolumeAsync(
                    projectId,
                    versionId,
                    row.WorkDirectionId,
                    row.PlannedVolume);
            }

            await LoadForecastAsync();
        }
        catch (Exception ex)
        {
            await LoadForecastAsync();
            ErrorMessage = $"Не удалось сохранить плановые объёмы: {ex.Message}";
        }
        finally
        {
            SavePlannedVolumesCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSavePlannedVolumes()
    {
        if (SelectedProjectId is null || SelectedVersionId is null || IsLoading)
            return false;

        return HasUnsavedPlannedVolumes;
    }

    partial void OnSelectedProjectIdChanged(int? value) => SavePlannedVolumesCommand.NotifyCanExecuteChanged();

    partial void OnSelectedVersionIdChanged(int? value) => SavePlannedVolumesCommand.NotifyCanExecuteChanged();

    partial void OnIsLoadingChanged(bool value) => SavePlannedVolumesCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (Report is null)
            return;

        await Task.CompletedTask;
    }

    private async Task LoadForecastAsync()
    {
        Interlocked.Increment(ref _previewGeneration);
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (SelectedProjectId is not int projectId || SelectedVersionId is not int versionId)
                return;

            var report = await _forecastService.GenerateForecastAsync(projectId, versionId);

            if (token.IsCancellationRequested)
                return;

            Report = report;
            SelectedProjectName = report.ProjectName;
            SelectedVersionName = report.VersionName;

            PopulateCharacteristics(report.ProjectCharacteristics);
            PopulatePlannedVolumes(report.PlannedVolumes);
            PopulateScenarios(report.Scenarios);
            PopulateInterpretations(report.Interpretations);
            PopulateTop3Disciplines(report.Top3Disciplines);
        }
        catch (OperationCanceledException)
        {
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

    private void PopulateCharacteristics(IReadOnlyDictionary<string, string> characteristics)
    {
        ProjectCharacteristics.Clear();
        foreach (var kvp in characteristics)
        {
            ProjectCharacteristics.Add(kvp);
        }
    }

    private void PopulatePlannedVolumes(IReadOnlyList<ForecastIicVolumeRowDto> volumes)
    {
        PlannedVolumes.Clear();
        var idsWithChildren = volumes
            .Where(v => v.ParentId.HasValue)
            .Select(v => v.ParentId!.Value)
            .ToHashSet();

        foreach (var volume in volumes)
        {
            var isLeaf = !idsWithChildren.Contains(volume.WorkDirectionId);
            var row = new ForecastIicVolumeRowModel(volume, isLeaf);
            row.PlannedVolumeEditedCallback = () =>
            {
                OnPropertyChanged(nameof(HasUnsavedPlannedVolumes));
                SavePlannedVolumesCommand.NotifyCanExecuteChanged();
                _ = SchedulePreviewRefreshAsync();
            };
            PlannedVolumes.Add(row);
        }

        SavePlannedVolumesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Пересчёт сценариев, интерпретации и ТОП по текущим значениям в таблице (без сохранения в БД).</summary>
    private async Task SchedulePreviewRefreshAsync()
    {
        var generation = Interlocked.Increment(ref _previewGeneration);
        try
        {
            await Task.Delay(300);
            if (generation != Volatile.Read(ref _previewGeneration) || IsLoading)
                return;

            if (SelectedProjectId is not int projectId || SelectedVersionId is not int versionId || PlannedVolumes.Count == 0)
                return;

            var nodeTotals = PlannedVolumes.ToDictionary(r => r.WorkDirectionId, r => r.PlannedVolume);
            var report = await _forecastService.GenerateForecastAsync(projectId, versionId, nodeTotals);

            if (generation != Volatile.Read(ref _previewGeneration))
                return;

            Report = report;
            PopulateScenarios(report.Scenarios);
            PopulateInterpretations(report.Interpretations);
            PopulateTop3Disciplines(report.Top3Disciplines);
        }
        catch (Exception)
        {
            // Предпросмотр не должен мешать вводу; ошибки полной загрузки по-прежнему в ErrorMessage.
        }
    }

    private void PopulateScenarios(IReadOnlyList<ForecastScenarioDto> scenarios)
    {
        Scenarios.Clear();
        foreach (var scenario in scenarios)
        {
            Scenarios.Add(scenario);
        }
    }

    private void PopulateInterpretations(IReadOnlyList<ScenarioInterpretationDto> interpretations)
    {
        Interpretations.Clear();
        foreach (var interpretation in interpretations)
        {
            Interpretations.Add(interpretation);
        }
    }

    private void PopulateTop3Disciplines(IReadOnlyList<TopDisciplineDto> disciplines)
    {
        Top3Disciplines.Clear();
        foreach (var discipline in disciplines)
        {
            Top3Disciplines.Add(discipline);
        }
    }
}
