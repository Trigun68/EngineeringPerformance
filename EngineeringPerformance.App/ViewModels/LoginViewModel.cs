using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Exceptions;
using EngineeringPerformance.Core.Services;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly IDbConnectionService _dbConnectionService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _passwordVisible;

    [ObservableProperty]
    private string? _windowsUsername;

    partial void OnWindowsUsernameChanged(string? value)
    {
        OnPropertyChanged(nameof(HasWindowsUsername));
    }

    public bool HasWindowsUsername => !string.IsNullOrEmpty(WindowsUsername);

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IAuthenticationService authService, IDbConnectionService dbConnectionService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _dbConnectionService = dbConnectionService;
        _logger = logger;
    }

    public UserDto? AuthenticatedUser { get; private set; }

    public void SetWindowsUsername(string? windowsUsername)
    {
        WindowsUsername = windowsUsername;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите имя пользователя и пароль";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            // Не логируем имя пользователя/пароль (PII).
            _logger.LogInformation("Login attempt");
            AuthenticatedUser = await _authService.LoginAsync(Username, Password);
            _logger.LogInformation("Login succeeded");
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (AuthenticationException ex)
        {
            // Доменное исключение — сообщаем пользователю обобщённо, без деталей.
            _logger.LogWarning(ex, "Authentication failed");
            ErrorMessage = "Неверное имя пользователя или пароль";
        }
        catch (Exception ex)
        {
            // Прочие ошибки (БД недоступна и т.п.) логируем подробно, пользователю — общая фраза.
            _logger.LogError(ex, "Unexpected error during login");
            ErrorMessage = "Не удалось выполнить вход. Подробности — в логах.";
        }
        finally
        {
            // Всегда стираем пароль из памяти после попытки входа, чтобы он не висел в свойстве VM
            // и не попал в потенциальную сериализацию состояния/heap-дамп.
            Password = string.Empty;
            IsBusy = false;
        }
    }

    public event EventHandler? LoginSucceeded;

    public Action? OpenDbSettingsWindow { get; set; }

    [RelayCommand]
    private void OpenDbSettings()
    {
        OpenDbSettingsWindow?.Invoke();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        PasswordVisible = !PasswordVisible;
    }
}
