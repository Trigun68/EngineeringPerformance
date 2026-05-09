using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using PhysicalVolumeType = EngineeringPerformance.Infrastructure.Data.PhysicalVolumeType;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectAnalysisVersionEditViewModel : ObservableObject
{
    private readonly IDirectoryService<ProjectAnalysisVersion> _service;
    private readonly IDirectoryService<Project> _projectService;
    private readonly IDirectoryCache<PhysicalVolumeType> _volumeTypeCache;
    private readonly IUserActionLogService _actionLog;
    private readonly CurrentUserHolder _currentUser;
    private Action<bool>? _closeDialog;
    private int? _preselectedProjectId;
    private ProjectAnalysisVersion? _preparedEntity;

    [ObservableProperty]
    private string _versionName = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private AnalysisDirection _selectedDirection = AnalysisDirection.Plan;

    [ObservableProperty]
    private int? _selectedPhysicalVolumeTypeId;

    [ObservableProperty]
    private ObservableCollection<PhysicalVolumeType> _physicalVolumeTypes = [];

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Today;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreLookupsReady))]
    private bool _isLookupsLoading;

    public bool AreLookupsReady => !IsLookupsLoading;

    [ObservableProperty]
    private string _windowTitle = "Новая версия анализа проекта";

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public ProjectAnalysisVersionEditViewModel(
        IDirectoryService<ProjectAnalysisVersion> service,
        IDirectoryService<Project> projectService,
        IDirectoryCache<PhysicalVolumeType> volumeTypeCache,
        IUserActionLogService actionLog,
        CurrentUserHolder currentUser)
    {
        _service = service;
        _projectService = projectService;
        _volumeTypeCache = volumeTypeCache;
        _actionLog = actionLog;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Мгновенно заполняет форму значениями. Справочник типов физ. объёмов
    /// и наименование проекта подгружаются <see cref="LoadLookupsAsync"/>.
    /// Параметр direction задаёт направление автоматически (Plan/Fact) — поле не показывается пользователю.
    /// </summary>
    public void PrepareSync(
        ProjectAnalysisVersion? entity,
        int? preselectedProjectId = null,
        AnalysisDirection? direction = null,
        Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        _preselectedProjectId = preselectedProjectId;
        _preparedEntity = entity;
        ErrorMessage = null;
        ProjectName = entity?.Project?.Name ?? string.Empty;
        IsLookupsLoading = true;

        if (entity is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование версии анализа проекта";
            EntityId = entity.Id;
            VersionName = entity.VersionName;
            Description = entity.Description;
            SelectedDirection = direction ?? entity.Direction;
            SelectedPhysicalVolumeTypeId = entity.PhysicalVolumeTypeId;
            CreatedAt = entity.CreatedAt.Date;
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новая версия анализа проекта";
            EntityId = 0;
            CreatedAt = DateTime.Today;
            SelectedDirection = direction ?? AnalysisDirection.Plan;
            SelectedPhysicalVolumeTypeId = null;
            VersionName = string.Empty;
            Description = null;
        }
    }

    public async Task LoadLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var volumeTypesTask = _volumeTypeCache.GetAllAsync(cancellationToken);

            Task<Project?> projectTask = Task.FromResult<Project?>(null);
            var projectId = _preparedEntity?.ProjectId ?? _preselectedProjectId;
            if (_preparedEntity?.Project is null && projectId is int pid && pid > 0)
            {
                projectTask = _projectService.GetByIdAsync(pid);
            }

            await Task.WhenAll(volumeTypesTask, projectTask);

            PhysicalVolumeTypes = new ObservableCollection<PhysicalVolumeType>(volumeTypesTask.Result);
            if (projectTask.Result is { } project)
            {
                ProjectName = project.Name;
            }
        }
        finally
        {
            IsLookupsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(VersionName))
        {
            ErrorMessage = "Наименование версии обязательно";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                var existing = await _service.GetByIdAsync(EntityId);
                if (existing is not null)
                {
                    existing.VersionName = VersionName.Trim();
                    existing.Description = Description?.Trim();
                    existing.Direction = SelectedDirection;
                    existing.PhysicalVolumeTypeId = SelectedPhysicalVolumeTypeId;
                    existing.ModifiedAt = DateTime.UtcNow;
                    await _service.UpdateAsync(existing);
                    await LogVersionActionAsync(UserActionType.VersionEdit, existing.Id, existing.ProjectId, existing.VersionName);
                }
            }
            else
            {
                if (_preselectedProjectId is not int projectId || projectId <= 0)
                {
                    ErrorMessage = "Не выбран проект для создания версии анализа";
                    return;
                }

                var entity = new ProjectAnalysisVersion
                {
                    VersionName = VersionName.Trim(),
                    Description = Description?.Trim(),
                    ProjectId = projectId,
                    Direction = SelectedDirection,
                    PhysicalVolumeTypeId = SelectedPhysicalVolumeTypeId,
                    CreatedAt = CreatedAt,
                };

                await _service.CreateAsync(entity);
                await LogVersionActionAsync(UserActionType.VersionCreate, entity.Id, entity.ProjectId, entity.VersionName);
            }

            CloseDialog(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private async Task LogVersionActionAsync(UserActionType actionType, int versionId, int projectId, string versionName)
    {
        var userId = _currentUser.User?.Id;
        if (userId is null) return;
        await _actionLog.LogAsync(
            userId.Value,
            actionType,
            entityType: "ProjectAnalysisVersion",
            entityId: versionId,
            description: $"ProjectId={projectId}, «{versionName}», direction={SelectedDirection}");
    }

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

public sealed class AnalysisDirectionItem
{
    public AnalysisDirection Direction { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
