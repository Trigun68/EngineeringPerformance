using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectEditViewModel : ObservableObject
{
    private const int AutoSaveDelayMs = 500;

    private readonly IDirectoryService<Project> _projectService;
    private readonly IDirectoryService<ProjectAnalysisVersion> _versionService;
    private readonly IProjectAnalysisVersionQueryService _versionQueryService;
    private readonly IDirectoryService<User> _userService;
    private readonly IProjectAnalysisVersionEditDialogService _versionEditDialog;
    private readonly IProjectParticipantEditDialogService _participantEditDialog;
    private readonly IDataImportDialogService _dataImportDialog;
    private readonly IIterationDeleteDialogService _iterationDeleteDialog;
    private readonly IProjectAdditionalFieldsDialogService _additionalFieldsDialog;
    private readonly IProjectCharacteristicsDialogService _characteristicsDialog;
    private readonly ProjectCharacteristicSelectionService _characteristicSelectionService;
    private readonly IPerformanceService _performanceService;
    private readonly IProjectVisibilityService _visibilityService;
    private readonly CurrentUserHolder _currentUserHolder;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserActionLogService _actionLog;
    private readonly DispatcherTimer _autoSaveTimer;

    private Action<bool>? _closeDialog;
    private bool _isInitialized;
    private bool _isSaving;
    private bool _hasChanges;
    private long _changeSequence;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _workNumber;
    [ObservableProperty] private string? _contractNumber;
    [ObservableProperty] private string? _calendarPlanNumber;
    [ObservableProperty] private string? _type;
    [ObservableProperty] private string? _customer;
    [ObservableProperty] private string? _contractor;
    [ObservableProperty] private string? _gip;
    [ObservableProperty] private string? _workStatus;
    [ObservableProperty] private DateTime? _workStart;
    [ObservableProperty] private string? _program;

    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _windowTitle = "Новый проект";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _entityId;

    [ObservableProperty] private ObservableCollection<ProjectAnalysisVersion> _analysisVersions = [];
    [ObservableProperty] private ProjectAnalysisVersion? _selectedVersion;

    [ObservableProperty] private ObservableCollection<ProjectParticipantItem> _projectParticipants = [];
    [ObservableProperty] private ProjectParticipantItem? _selectedProjectParticipant;

    [ObservableProperty] private ObservableCollection<PerformanceItem> _performanceData = [];
    [ObservableProperty] private PerformanceItem? _selectedPerformanceItem;

    /// <summary>Среднеарифметическое значений PD% выбранных вариантов характеристик проекта.</summary>
    [ObservableProperty] private decimal? _projectDeviationPercent;

    public IReadOnlyList<string> WorkStatusItems { get; } =
    [
        "Черновик",
        "в работе",
        "завершен",
        "отменен",
    ];

    public event EventHandler? PerformanceDataLoaded;

    public ProjectEditViewModel(
        IDirectoryService<Project> projectService,
        IDirectoryService<ProjectAnalysisVersion> versionService,
        IProjectAnalysisVersionQueryService versionQueryService,
        IDirectoryService<User> userService,
        IProjectAnalysisVersionEditDialogService versionEditDialog,
        IProjectParticipantEditDialogService participantEditDialog,
        IDataImportDialogService dataImportDialog,
        IIterationDeleteDialogService iterationDeleteDialog,
        IProjectAdditionalFieldsDialogService additionalFieldsDialog,
        IProjectCharacteristicsDialogService characteristicsDialog,
        ProjectCharacteristicSelectionService characteristicSelectionService,
        IPerformanceService performanceService,
        IProjectVisibilityService visibilityService,
        CurrentUserHolder currentUserHolder,
        IDbContextFactory<AppDbContext> contextFactory,
        IServiceProvider serviceProvider,
        IUserActionLogService actionLog)
    {
        _projectService = projectService;
        _versionService = versionService;
        _versionQueryService = versionQueryService;
        _userService = userService;
        _versionEditDialog = versionEditDialog;
        _participantEditDialog = participantEditDialog;
        _dataImportDialog = dataImportDialog;
        _iterationDeleteDialog = iterationDeleteDialog;
        _additionalFieldsDialog = additionalFieldsDialog;
        _characteristicsDialog = characteristicsDialog;
        _characteristicSelectionService = characteristicSelectionService;
        _performanceService = performanceService;
        _visibilityService = visibilityService;
        _currentUserHolder = currentUserHolder;
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _actionLog = actionLog;

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AutoSaveDelayMs),
        };
        _autoSaveTimer.Tick += AutoSaveTimerElapsed;
    }

    public async Task InitializeAsync(Project? item = null, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;

        if (item is not null)
        {
            EntityId = item.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование проекта";
            Code = item.Code;
            Name = item.Name;
            WorkNumber = item.WorkNumber;
            ContractNumber = item.ContractNumber;
            CalendarPlanNumber = item.CalendarPlanNumber;
            Type = item.Type;
            Customer = item.Customer;
            Contractor = item.Contractor;
            Gip = item.Gip;
            WorkStatus = item.WorkStatus;
            WorkStart = item.WorkStart?.ToDateTime(TimeOnly.MinValue);
            Program = item.Program;

            await LoadAnalysisVersionsAsync();
            await LoadProjectParticipantsAsync();
            await LoadProjectDeviationAsync();
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Новый проект";
            AnalysisVersions.Clear();
            ProjectParticipants.Clear();
            ProjectDeviationPercent = null;
        }

        _hasChanges = false;
        _isInitialized = true;
    }

    private async Task LoadAnalysisVersionsAsync()
    {
        if (EntityId <= 0)
        {
            AnalysisVersions = [];
            SelectedVersion = null;
            return;
        }

        var versions = await _versionQueryService.GetByProjectAsync(EntityId, includeArchived: true);
        AnalysisVersions = new ObservableCollection<ProjectAnalysisVersion>(versions);

        if (SelectedVersion is null && AnalysisVersions.Count > 0)
        {
            SelectedVersion = AnalysisVersions[0];
        }
    }

    private async Task LoadProjectParticipantsAsync()
    {
        if (EntityId <= 0)
        {
            ProjectParticipants.Clear();
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var participants = await context.ProjectPermissions
                .AsNoTracking()
                .Include(pp => pp.User)
                .ThenInclude(u => u.Role)
                .Where(pp => pp.ProjectId == EntityId)
                .Select(pp => new ProjectParticipantItem
                {
                    Id = pp.User.Id,
                    UserName = pp.User.DisplayName,
                    Role = pp.User.Role.Name,
                    AccessLevel = pp.Level,
                })
                .ToListAsync();

            ProjectParticipants.Clear();
            foreach (var participant in participants)
            {
                ProjectParticipants.Add(participant);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки участников проекта: {ex.Message}";
        }
    }

    partial void OnSelectedVersionChanged(ProjectAnalysisVersion? value)
    {
        if (value is not null && EntityId > 0)
        {
            _ = LoadPerformanceDataAsync(EntityId, value.Id);
        }
        else
        {
            PerformanceData.Clear();
        }
    }

    private async Task LoadPerformanceDataAsync(int projectId, int versionId)
    {
        try
        {
            var data = await _performanceService.GetPerformanceDataAsync(projectId, versionId);
            PerformanceData.Clear();
            foreach (var row in data)
            {
                PerformanceData.Add(new PerformanceItem
                {
                    Id = row.WorkDirectionId,
                    ParentId = row.ParentId,
                    Name = row.WorkDirectionName,
                    NormUnitName = row.NormUnitName ?? string.Empty,
                    FactPhysicalVolume = row.FactPhysicalVolume,
                    FactLaborHours = row.FactLaborHours,
                    FactProductivity = row.FactProductivity,
                    FactEstimate = row.FactEstimate,
                    NormProductivity = row.NormProductivity,
                    NormEstimate = row.NormEstimate,
                    NormStdDev = row.NormStdDev,
                    ZScore = row.ZScore,
                    Interpretation = row.Interpretation?.ToRussian() ?? string.Empty,
                });
            }

            PerformanceDataLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных производительности: {ex.Message}";
        }
    }

    partial void OnCodeChanged(string value) => OnFieldChanged();
    partial void OnNameChanged(string value) => OnFieldChanged();
    partial void OnWorkNumberChanged(string? value) => OnFieldChanged();
    partial void OnContractNumberChanged(string? value) => OnFieldChanged();
    partial void OnCalendarPlanNumberChanged(string? value) => OnFieldChanged();
    partial void OnTypeChanged(string? value) => OnFieldChanged();
    partial void OnCustomerChanged(string? value) => OnFieldChanged();
    partial void OnContractorChanged(string? value) => OnFieldChanged();
    partial void OnGipChanged(string? value) => OnFieldChanged();
    partial void OnWorkStatusChanged(string? value) => OnFieldChanged();
    partial void OnWorkStartChanged(DateTime? value) => OnFieldChanged();
    partial void OnProgramChanged(string? value) => OnFieldChanged();

    private void OnFieldChanged()
    {
        if (!_isInitialized) return;

        _hasChanges = true;
        _changeSequence++;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async void AutoSaveTimerElapsed(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();

        if (!_hasChanges || _isSaving) return;

        await SaveProjectAsync();
    }

    public void ApplyAdditionalFields(
        string? contractNumber,
        string? calendarPlanNumber,
        string? type,
        string? customer,
        string? contractor,
        string? program)
    {
        ContractNumber = contractNumber;
        CalendarPlanNumber = calendarPlanNumber;
        Type = type;
        Customer = customer;
        Contractor = contractor;
        Program = program;
    }

    public Task PersistProjectAsync() => SaveProjectAsync();

    /// <summary>Вызывается при закрытии окна: дожидается текущего сохранения и при необходимости сохраняет основные данные проекта в БД.</summary>
    public async Task<bool> FinishClosingAsync()
    {
        _autoSaveTimer.Stop();

        while (_isSaving)
        {
            await Task.Delay(25);
        }

        if (_hasChanges)
        {
            await SaveProjectAsync();
        }

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            return false;
        }

        if (_hasChanges)
        {
            ErrorMessage = "Укажите код и название проекта, чтобы сохранить изменения.";
            return false;
        }

        return true;
    }

    private async Task SaveProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        _isSaving = true;
        var saveStartedSequence = _changeSequence;

        try
        {
            if (IsEditMode && EntityId > 0)
            {
                var existing = await _projectService.GetByIdAsync(EntityId);
                if (existing is null)
                {
                    ErrorMessage = "Проект не найден";
                    return;
                }

                existing.Code = Code.Trim();
                existing.Name = Name.Trim();
                existing.WorkNumber = WorkNumber?.Trim();
                existing.ContractNumber = ContractNumber?.Trim();
                existing.CalendarPlanNumber = CalendarPlanNumber?.Trim();
                existing.Type = Type?.Trim();
                existing.Customer = Customer?.Trim();
                existing.Contractor = Contractor?.Trim();
                existing.Gip = Gip?.Trim();
                existing.WorkStatus = WorkStatus?.Trim();
                existing.WorkStart = WorkStart.HasValue ? DateOnly.FromDateTime(WorkStart.Value) : null;
                existing.Program = Program?.Trim();
                existing.ModifiedAt = DateTime.UtcNow;
                await _projectService.UpdateAsync(existing);
                await LogAsync(UserActionType.ProjectEdit, EntityId, $"{existing.Code} {existing.Name}");
            }
            else
            {
                var item = new Project
                {
                    Code = Code.Trim(),
                    Name = Name.Trim(),
                    WorkNumber = WorkNumber?.Trim(),
                    ContractNumber = ContractNumber?.Trim(),
                    CalendarPlanNumber = CalendarPlanNumber?.Trim(),
                    Type = Type?.Trim(),
                    Customer = Customer?.Trim(),
                    Contractor = Contractor?.Trim(),
                    Gip = Gip?.Trim(),
                    WorkStatus = WorkStatus?.Trim(),
                    WorkStart = WorkStart.HasValue ? DateOnly.FromDateTime(WorkStart.Value) : null,
                    Program = Program?.Trim(),
                };
                var created = await _projectService.CreateAsync(item);
                EntityId = created.Id;
                IsEditMode = true;
                WindowTitle = "Редактирование проекта";
                await LogAsync(UserActionType.ProjectCreate, created.Id, $"{created.Code} {created.Name}");

                // V2-C: автору-не-админу нужна явная запись в ProjectPermission,
                // иначе GetAccessibleProjectsAsync отсечёт его же проект сразу
                // после создания. Админа фильтр пропускает в любом случае,
                // но его роль всё равно покрывается отсутствием записи.
                if (_currentUserHolder.User is { } currentUser)
                {
                    await _visibilityService.AddUserToProjectAsync(created.Id, currentUser.Id);
                }

                await LoadAnalysisVersionsAsync();
                await LoadProjectParticipantsAsync();
            }

            if (_changeSequence == saveStartedSequence)
            {
                _hasChanges = false;
            }
            else
            {
                _hasChanges = true;
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
        }
    }

    [RelayCommand]
    private void OpenAdditionalFields()
    {
        SetDialogOwner();
        _additionalFieldsDialog.ShowDialog(this);
    }

    [RelayCommand]
    private async Task OpenCharacteristicsAsync()
    {
        if (EntityId <= 0)
        {
            ErrorMessage = "Сохраните проект перед редактированием характеристик";
            return;
        }

        SetDialogOwner();
        var saved = await _characteristicsDialog.ShowAsync(EntityId);
        if (saved)
        {
            await LoadProjectDeviationAsync();
        }
    }

    /// <summary>
    /// Грузит выбранные варианты характеристик проекта и пересчитывает
    /// среднеарифметическое PD% (отображается над кнопкой «Характеристики»).
    /// </summary>
    private async Task LoadProjectDeviationAsync()
    {
        if (EntityId <= 0)
        {
            ProjectDeviationPercent = null;
            return;
        }

        var rows = await _characteristicSelectionService.GetByProjectAsync(EntityId);
        var values = rows.Where(r => r.ValuePercent.HasValue).Select(r => r.ValuePercent!.Value).ToList();
        ProjectDeviationPercent = values.Count == 0
            ? null
            : Math.Round(values.Average(), 2, MidpointRounding.AwayFromZero);
    }

    [RelayCommand]
    private async Task AddVersionAsync()
    {
        var result = await _versionEditDialog.ShowEditDialogAsync(null, EntityId);
        if (result)
        {
            await LoadAnalysisVersionsAsync();
        }
    }

    [RelayCommand]
    private async Task EditVersionAsync()
    {
        if (SelectedVersion is null) return;

        var result = await _versionEditDialog.ShowEditDialogAsync(SelectedVersion, EntityId);
        if (result)
        {
            await LoadAnalysisVersionsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteVersionAsync()
    {
        if (SelectedVersion is null) return;

        var confirm = MessageBox.Show(
            $"Удалить версию \"{SelectedVersion.VersionName}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            var versionId = SelectedVersion.Id;
            var versionName = SelectedVersion.VersionName;
            await _versionService.DeleteAsync(versionId);
            await LogAsync(UserActionType.VersionDelete, versionId, $"ProjectId={EntityId}, «{versionName}»", entityType: "ProjectAnalysisVersion");
            await LoadAnalysisVersionsAsync();
        }
    }

    [RelayCommand]
    private async Task AddParticipantAsync()
    {
        if (EntityId <= 0) return;

        SetDialogOwner();
        var result = await _participantEditDialog.ShowEditDialogAsync(EntityId);
        if (result)
        {
            await LoadProjectParticipantsAsync();
        }
    }

    [RelayCommand]
    private async Task EditParticipantAsync()
    {
        if (SelectedProjectParticipant is null || EntityId <= 0) return;

        var user = await _userService.GetByIdAsync(SelectedProjectParticipant.Id);

        SetDialogOwner();
        var result = await _participantEditDialog.ShowEditDialogAsync(EntityId, user);
        if (result)
        {
            await LoadProjectParticipantsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteParticipantAsync()
    {
        if (SelectedProjectParticipant is null || EntityId <= 0) return;

        var confirm = MessageBox.Show(
            $"Удалить участника \"{SelectedProjectParticipant.UserName}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var permission = await context.ProjectPermissions
                    .FirstOrDefaultAsync(pp => pp.ProjectId == EntityId && pp.UserId == SelectedProjectParticipant.Id);

                if (permission is not null)
                {
                    context.ProjectPermissions.Remove(permission);
                    await context.SaveChangesAsync();
                }

                await LoadProjectParticipantsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка удаления участника: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ImportPhysicalVolumesAsync()
    {
        await ImportDataAsync(DataImportType.PhysicalVolume);
    }

    [RelayCommand]
    private async Task DeletePhysicalVolumesAsync()
    {
        await DeleteDataAsync(DataImportType.PhysicalVolume);
    }

    [RelayCommand]
    private async Task ImportLaborIntensitiesAsync()
    {
        await ImportDataAsync(DataImportType.LaborIntensity);
    }

    [RelayCommand]
    private async Task DeleteLaborIntensitiesAsync()
    {
        await DeleteDataAsync(DataImportType.LaborIntensity);
    }

    /// <summary>
    /// Открывает окно «Анализ версии проекта» для текущей выбранной версии.
    /// Окно — модальное; данные подгружает сам через ILoadableViewModel.LoadAsync.
    /// </summary>
    [RelayCommand]
    private void OpenVersionAnalysis()
    {
        if (EntityId <= 0)
        {
            ErrorMessage = "Сохраните проект перед открытием отчёта";
            return;
        }
        if (SelectedVersion is null)
        {
            ErrorMessage = "Выберите версию анализа для отчёта";
            return;
        }

        var window = _serviceProvider.GetRequiredService<ProjectVersionAnalysisWindow>();
        var vm = _serviceProvider.GetRequiredService<ProjectVersionAnalysisViewModel>();
        vm.ProjectId = EntityId;
        vm.VersionId = SelectedVersion.Id;
        vm.OwnerWindow = window;
        window.DataContext = vm;
        window.Owner = GetOwnerWindow();
        _ = vm.LoadAsync();
        window.ShowDialog();
    }

    private async Task ImportDataAsync(DataImportType dataType)
    {
        if (SelectedVersion is null || EntityId <= 0)
        {
            ErrorMessage = "Выберите версию анализа для импорта";
            return;
        }

        SetDialogOwner();
        var result = await _dataImportDialog.ShowImportDialogAsync(EntityId, SelectedVersion.Id, dataType);

        if (result is { Success: true })
        {
            await LoadPerformanceDataAsync(EntityId, SelectedVersion.Id);
        }
    }

    private async Task DeleteDataAsync(DataImportType dataType)
    {
        if (SelectedVersion is null || EntityId <= 0)
        {
            ErrorMessage = "Выберите версию анализа для удаления";
            return;
        }

        SetDialogOwner();
        var result = await _iterationDeleteDialog.ShowDeleteDialogAsync(EntityId, SelectedVersion.Id, dataType);

        if (result)
        {
            await LoadPerformanceDataAsync(EntityId, SelectedVersion.Id);
        }
    }

    private async Task LogAsync(UserActionType actionType, int entityId, string description, string entityType = "Project")
    {
        var userId = _currentUserHolder.User?.Id;
        if (userId is null) return;
        await _actionLog.LogAsync(userId.Value, actionType, entityType: entityType, entityId: entityId, description: description);
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (!await FinishClosingAsync())
        {
            return;
        }

        CloseDialog(false);
    }

    private Window? GetOwnerWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
    }

    private void SetDialogOwner()
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
        {
            _participantEditDialog.SetOwnerWindow(owner);
            _dataImportDialog.SetOwnerWindow(owner);
            _iterationDeleteDialog.SetOwnerWindow(owner);
            _additionalFieldsDialog.SetOwnerWindow(owner);
            _characteristicsDialog.SetOwnerWindow(owner);
        }
    }

    private void CloseDialog(bool result)
    {
        _autoSaveTimer.Stop();

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

public sealed class ProjectParticipantItem
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Read;
    public string AccessLevelDisplay => AccessLevel.ToRussian();
}

public sealed class PerformanceItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string NormUnitName { get; set; } = string.Empty;

    public decimal? FactPhysicalVolume { get; set; }
    public decimal? FactLaborHours { get; set; }
    public decimal? FactProductivity { get; set; }
    public decimal? FactEstimate { get; set; }
    public decimal? NormProductivity { get; set; }
    public decimal? NormEstimate { get; set; }
    public decimal? NormStdDev { get; set; }
    public decimal? ZScore { get; set; }
    public string Percentile { get; set; } = string.Empty;
    public string Interpretation { get; set; } = string.Empty;
}
