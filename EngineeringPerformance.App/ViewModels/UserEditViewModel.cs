using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class UserEditViewModel : ObservableObject
{
    private readonly IDirectoryService<User> _userService;
    private readonly IDirectoryService<Role> _roleService;
    private Action<bool>? _closeDialog;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private int _selectedRoleId;

    [ObservableProperty]
    private ObservableCollection<Role> _roles = [];

    [ObservableProperty]
    private string? _windowsUsername;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _windowTitle = "Новый пользователь";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _entityId;

    public UserEditViewModel(
        IDirectoryService<User> userService,
        IDirectoryService<Role> roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    public async Task InitializeAsync(User? user = null, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ErrorMessage = null;
        var allRoles = await _roleService.GetAllAsync(false);
        Roles = new ObservableCollection<Role>(allRoles);

        if (user is not null)
        {
            EntityId = user.Id;
            IsEditMode = true;
            WindowTitle = "Редактирование пользователя";
            Username = user.Username;
            DisplayName = user.DisplayName;
            SelectedRoleId = user.RoleId;
            WindowsUsername = user.WindowsUsername;
            IsActive = user.IsActive;
        }
        else if (Roles.Count > 0)
        {
            SelectedRoleId = Roles[0].Id;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Логин и имя обязательны";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                var existing = await _userService.GetByIdAsync(EntityId);
                if (existing is null)
                {
                    ErrorMessage = "Пользователь не найден";
                    return;
                }

                var duplicate = (await _userService.GetAllAsync(true))
                    .FirstOrDefault(u => u.Id != EntityId && u.Username.Equals(Username, StringComparison.OrdinalIgnoreCase));
                if (duplicate is not null)
                {
                    ErrorMessage = "Пользователь с таким логином уже существует";
                    return;
                }

                existing.Username = Username.Trim();
                existing.DisplayName = DisplayName.Trim();
                existing.RoleId = SelectedRoleId;
                existing.WindowsUsername = WindowsUsername?.Trim();
                existing.IsActive = IsActive;
                existing.ModifiedAt = DateTime.UtcNow;
                await _userService.UpdateAsync(existing);
            }
            else
            {
                var allUsers = await _userService.GetAllAsync(true);
                if (allUsers.Any(u => u.Username.Equals(Username.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorMessage = "Пользователь с таким логином уже существует";
                    return;
                }

                var user = new User
                {
                    Username = Username.Trim(),
                    DisplayName = DisplayName.Trim(),
                    RoleId = SelectedRoleId,
                    WindowsUsername = WindowsUsername?.Trim(),
                    IsActive = IsActive,
                };

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                }

                await _userService.CreateAsync(user);
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
