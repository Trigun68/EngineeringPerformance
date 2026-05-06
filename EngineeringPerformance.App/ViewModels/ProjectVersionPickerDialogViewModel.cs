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

public sealed partial class ProjectVersionPickerDialogViewModel : ObservableObject
{
    private readonly IProjectAnalysisVersionQueryService _queryService;
    private readonly IDirectoryService<ProjectAnalysisVersion> _versionService;
    private readonly IProjectAnalysisVersionEditDialogService _editDialog;
    private Action<bool>? _closeDialog;

    public int ProjectId { get; private set; }
    public string ProjectName { get; private set; } = string.Empty;
    public AnalysisDirection Direction { get; private set; }

    [ObservableProperty] private string _windowTitle = "Версии анализа";
    [ObservableProperty] private ObservableCollection<ProjectAnalysisVersion> _versions = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditVersionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVersionCommand))]
    private ProjectAnalysisVersion? _selectedVersion;
    [ObservableProperty] private string? _errorMessage;

    public ProjectVersionPickerDialogViewModel(
        IProjectAnalysisVersionQueryService queryService,
        IDirectoryService<ProjectAnalysisVersion> versionService,
        IProjectAnalysisVersionEditDialogService editDialog)
    {
        _queryService = queryService;
        _versionService = versionService;
        _editDialog = editDialog;
    }

    public async Task InitializeAsync(
        int projectId,
        string projectName,
        AnalysisDirection direction,
        Action<bool>? closeDialog = null)
    {
        ProjectId = projectId;
        ProjectName = projectName;
        Direction = direction;
        _closeDialog = closeDialog;
        ErrorMessage = null;

        WindowTitle = direction == AnalysisDirection.Plan
            ? $"Версии плана — {projectName}"
            : $"Версии факта — {projectName}";

        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            var all = await _queryService.GetByProjectAsync(ProjectId, includeArchived: false);
            var filtered = all.Where(v => v.Direction == Direction).ToList();
            Versions = new ObservableCollection<ProjectAnalysisVersion>(filtered);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки версий: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Select() => CloseDialog(true);
    private bool CanSelect() => SelectedVersion is not null;

    [RelayCommand]
    private async Task AddVersionAsync()
    {
        var owner = GetOwner();
        if (owner is not null) _editDialog.SetOwnerWindow(owner);
        var saved = await _editDialog.ShowEditDialogAsync(null, ProjectId);
        if (saved) await ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task EditVersionAsync()
    {
        if (SelectedVersion is null) return;
        var owner = GetOwner();
        if (owner is not null) _editDialog.SetOwnerWindow(owner);
        var saved = await _editDialog.ShowEditDialogAsync(SelectedVersion, ProjectId);
        if (saved) await ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task DeleteVersionAsync()
    {
        if (SelectedVersion is null) return;
        var confirm = MessageBox.Show(
            $"Удалить версию \"{SelectedVersion.VersionName}\"?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _versionService.DeleteAsync(SelectedVersion.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private Window? GetOwner() =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
            _closeDialog(result);
        else
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (GetOwner() is Window win)
                    win.DialogResult = result;
            });
    }
}
