using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectPickerDialogViewModel : ObservableObject
{
    private readonly IDirectoryService<Project> _projectService;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private ObservableCollection<ProjectPickerItem> _projects = [];
    [ObservableProperty] private ObservableCollection<ProjectPickerItem> _filteredProjects = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showArchived;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ArchiveProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnarchiveProjectCommand))]
    private ProjectPickerItem? _selectedProject;
    [ObservableProperty] private string? _errorMessage;

    public ProjectPickerDialogViewModel(IDirectoryService<Project> projectService)
    {
        _projectService = projectService;
    }

    public async Task InitializeAsync(Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            var all = await _projectService.GetAllAsync(includeArchived: ShowArchived);
            Projects = new ObservableCollection<ProjectPickerItem>(
                all.Select(p => new ProjectPickerItem(p.Id, p.Code, p.Name, p.IsArchived)));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки проектов: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        var term = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? Projects
            : new ObservableCollection<ProjectPickerItem>(
                Projects.Where(p =>
                    p.Name.ToLowerInvariant().Contains(term) ||
                    p.Code.ToLowerInvariant().Contains(term)));
        FilteredProjects = source;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnShowArchivedChanged(bool value) => _ = ReloadAsync();

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Select() => CloseDialog(true);

    private bool CanSelect() => SelectedProject is not null;

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        var vm = new ProjectQuickEditDialogViewModel(null);
        var win = new Views.ProjectQuickEditDialog { DataContext = vm };
        vm.Initialize(result => win.Dispatcher.Invoke(() => win.DialogResult = result));
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        if (owner is not null) win.Owner = owner;
        if (win.ShowDialog() != true) return;

        try
        {
            var item = new Project { Code = vm.Code.Trim(), Name = vm.Name.Trim() };
            await _projectService.CreateAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка создания проекта: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task EditProjectAsync()
    {
        if (SelectedProject is null) return;
        var existing = await _projectService.GetByIdAsync(SelectedProject.Id);
        if (existing is null) return;

        var vm = new ProjectQuickEditDialogViewModel(existing);
        var win = new Views.ProjectQuickEditDialog { DataContext = vm };
        vm.Initialize(result => win.Dispatcher.Invoke(() => win.DialogResult = result));
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        if (owner is not null) win.Owner = owner;
        if (win.ShowDialog() != true) return;

        try
        {
            existing.Code = vm.Code.Trim();
            existing.Name = vm.Name.Trim();
            await _projectService.UpdateAsync(existing);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject is null) return;
        var confirm = MessageBox.Show(
            $"Удалить проект \"{SelectedProject.Name}\"? Это действие необратимо.",
            "Подтверждение удаления",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _projectService.DeleteAsync(SelectedProject.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanArchive))]
    private async Task ArchiveProjectAsync()
    {
        if (SelectedProject is null) return;
        try
        {
            var item = await _projectService.GetByIdAsync(SelectedProject.Id);
            if (item is null) return;
            item.IsArchived = true;
            await _projectService.UpdateAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка архивации: {ex.Message}";
        }
    }

    private bool CanArchive() => SelectedProject is { IsArchived: false };

    [RelayCommand(CanExecute = nameof(CanUnarchive))]
    private async Task UnarchiveProjectAsync()
    {
        if (SelectedProject is null) return;
        try
        {
            var item = await _projectService.GetByIdAsync(SelectedProject.Id);
            if (item is null) return;
            item.IsArchived = false;
            await _projectService.UpdateAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка восстановления из архива: {ex.Message}";
        }
    }

    private bool CanUnarchive() => SelectedProject is { IsArchived: true };

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
            _closeDialog(result);
        else
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this) is Window win)
                    win.DialogResult = result;
            });
    }
}

public sealed class ProjectPickerItem(int id, string code, string name, bool isArchived)
{
    public int Id { get; } = id;
    public string Code { get; } = code;
    public string Name { get; } = name;
    public bool IsArchived { get; } = isArchived;
}
