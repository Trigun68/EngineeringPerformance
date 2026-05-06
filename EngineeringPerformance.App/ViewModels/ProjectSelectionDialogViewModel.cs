using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectSelectionDialogViewModel : ObservableObject
{
    private readonly IDirectoryService<Project> _projectService;
    private readonly IProjectAnalysisVersionQueryService _versionQueryService;
    private readonly IProjectAnalysisVersionEditDialogService _versionEditDialogService;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private ObservableCollection<ProjectItem> _projects = [];

    [ObservableProperty]
    private ObservableCollection<ProjectItem> _filteredProjects = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    private ProjectItem? _selectedProject;

    partial void OnSelectedProjectChanged(ProjectItem? value)
    {
        _ = LoadVersionsAsync();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    private int? _selectedVersionId;

    [ObservableProperty]
    private ObservableCollection<VersionItem> _availableVersions = [];

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _versionSelectionHint;

    [ObservableProperty]
    private AnalysisDirection _directionFilter = AnalysisDirection.Fact;

    public ProjectSelectionDialogViewModel(
        IDirectoryService<Project> projectService,
        IProjectAnalysisVersionQueryService versionQueryService,
        IProjectAnalysisVersionEditDialogService versionEditDialogService)
    {
        _projectService = projectService;
        _versionQueryService = versionQueryService;
        _versionEditDialogService = versionEditDialogService;
    }

    public async Task InitializeAsync(Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        VersionSelectionHint = null;
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var allProjects = await _projectService.GetAllAsync(includeArchived: false);
            Projects = new ObservableCollection<ProjectItem>(
                allProjects.Select(p => new ProjectItem(p.Id, p.Name, p.Code)));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки проектов: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        var filter = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(filter))
        {
            FilteredProjects = new ObservableCollection<ProjectItem>(Projects);
        }
        else
        {
            FilteredProjects = new ObservableCollection<ProjectItem>(
                Projects.Where(p =>
                    p.Name.ToLowerInvariant().Contains(filter) ||
                    p.Code.ToLowerInvariant().Contains(filter)));
        }
    }

    private async Task LoadVersionsAsync()
    {
        AvailableVersions.Clear();
        SelectedVersionId = null;
        VersionSelectionHint = null;

        if (SelectedProject is null)
            return;

        try
        {
            var projectVersions = await _versionQueryService.GetByProjectAsync(
                SelectedProject.Id,
                includeArchived: false);

            var versions = projectVersions
                .Where(v => v.Direction == DirectionFilter)
                .Select(v => new VersionItem(v.Id, v.VersionName, v.DirectionName, v.CreatedAt))
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            foreach (var version in versions)
            {
                AvailableVersions.Add(version);
            }

            if (versions.Count > 0)
            {
                SelectedVersionId = versions[0].Id;
            }
            else
            {
                VersionSelectionHint = DirectionFilter == AnalysisDirection.Plan
                    ? "Нет версий анализа с направлением «План» для этого проекта. Создайте версию прогноза или выберите другой проект."
                    : "Нет версий анализа с направлением «Факт» для этого проекта. Создайте версию или выберите другой проект.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки версий: {ex.Message}";
        }
        finally
        {
            SelectCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Select()
    {
        CloseDialog(true);
    }

    private bool CanSelect()
    {
        return SelectedProject is not null && SelectedVersionId is not null;
    }

    [RelayCommand]
    private async Task CreateNewVersionAsync()
    {
        if (SelectedProject is null)
        {
            ErrorMessage = "Сначала выберите проект.";
            return;
        }

        ErrorMessage = null;
        var saved = await _versionEditDialogService.ShowEditDialogAsync(null, SelectedProject.Id);
        if (saved)
        {
            await LoadVersionsAsync();
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
        {
            _closeDialog(result);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                {
                    window.DialogResult = result;
                }
            });
        }
    }
}

public sealed class ProjectItem(int id, string name, string code)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Code { get; } = code;
}

public sealed class VersionItem(int id, string name, string directionName, DateTime createdAt)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string DirectionName { get; } = directionName;
    public DateTime CreatedAt { get; } = createdAt;
}
