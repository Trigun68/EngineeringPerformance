using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectParticipantEditViewModel : ObservableObject
{
    private readonly IDirectoryService<User> _userService;
    private readonly IProjectVisibilityService _visibilityService;
    private readonly IAccessControlService _accessControlService;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private int _projectId;
    [ObservableProperty] private ObservableCollection<UserDisplayItem> _availableParticipants = [];
    [ObservableProperty] private UserDisplayItem? _selectedParticipant;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<UserDisplayItem> _filteredParticipants = [];
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _windowTitle = "Добавить участника проекта";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int? _existingUserId;

    public ProjectParticipantEditViewModel(
        IDirectoryService<User> userService,
        IProjectVisibilityService visibilityService,
        IAccessControlService accessControlService)
    {
        _userService = userService;
        _visibilityService = visibilityService;
        _accessControlService = accessControlService;
    }

    public async Task InitializeAsync(int projectId, User? existingUser = null, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ProjectId = projectId;
        ErrorMessage = null;
        SearchText = string.Empty;

        // V2-C: участником проекта может стать любой активный не-админ пользователь.
        // Раньше фильтр по UserRole.ProjectManager отрезал Observer и кастомные роли.
        var allUsers = await _userService.GetAllAsync(false);
        var eligibleUsers = allUsers
            .Where(u => u.IsActive && u.RoleId != (int)UserRole.Administrator)
            .Select(u => new UserDisplayItem(u.Id, u.DisplayName, u.Username))
            .ToList();

        AvailableParticipants = new ObservableCollection<UserDisplayItem>(eligibleUsers);
        ApplyFilter();

        if (existingUser is not null)
        {
            IsEditMode = true;
            WindowTitle = "Редактирование участника проекта";
            ExistingUserId = existingUser.Id;
            SelectedParticipant = AvailableParticipants.FirstOrDefault(m => m.Id == existingUser.Id);
        }
        else
        {
            IsEditMode = false;
            WindowTitle = "Добавить участника проекта";
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? AvailableParticipants.ToList()
            : AvailableParticipants
                .Where(m => m.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        FilteredParticipants = new ObservableCollection<UserDisplayItem>(filtered);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedParticipant is null)
        {
            ErrorMessage = "Выберите пользователя";
            return;
        }

        try
        {
            // При смене пользователя в edit-режиме старую запись удаляем — иначе у проекта
            // оставались бы две привязки (старая + новая) с разными уровнями доступа.
            if (IsEditMode && ExistingUserId.HasValue && ExistingUserId.Value != SelectedParticipant.Id)
            {
                await _visibilityService.RemoveUserFromProjectAsync(ProjectId, ExistingUserId.Value);
            }

            // Уровень доступа участника больше не выбирается вручную — он полностью
            // определяется ролью пользователя и настройками доступа роли к объекту
            // «Проекты» (AccessRights). Запись ProjectPermission — это флаг участия
            // в проекте; значение Level сохраняется только для обратной совместимости
            // с отображением в таблице участников.
            var level = await ResolveRoleAccessLevelAsync(SelectedParticipant.Id);
            await _visibilityService.AddOrUpdateUserPermissionAsync(ProjectId, SelectedParticipant.Id, level);
            CloseDialog(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private async Task<AccessLevel> ResolveRoleAccessLevelAsync(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
        {
            return AccessLevel.Read;
        }

        var rights = await _accessControlService.GetAllAccessRightsAsync();
        var match = rights.FirstOrDefault(r =>
            r.RoleId == user.RoleId &&
            string.Equals(r.ObjectName, "Projects", StringComparison.Ordinal));

        return match?.AccessLevel ?? AccessLevel.Read;
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

public sealed record UserDisplayItem(int Id, string DisplayName, string Username);
