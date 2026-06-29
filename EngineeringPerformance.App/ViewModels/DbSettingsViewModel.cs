using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class DbSettingsViewModel : ObservableObject
{
    private readonly IDbConnectionService _dbConnectionService;
    private readonly IAppSettings _appSettings;

    [ObservableProperty]
    private string _server = @".\SQLEXPRESS";

    [ObservableProperty]
    private string _database = "EngineeringPerformance";

    [ObservableProperty]
    private bool _useWindowsAuth = true;

    [ObservableProperty]
    private string _dbUsername = string.Empty;

    [ObservableProperty]
    private string _dbPassword = string.Empty;

    /// <summary>
    /// Доверять серверному сертификату без валидации. Безопасно только для LocalDB/локальных экземпляров.
    /// Для удалённых серверов должен быть выключен (иначе MitM по TLS).
    /// </summary>
    [ObservableProperty]
    private bool _trustServerCertificate;

    [ObservableProperty]
    private bool _autoLoginEnabled;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private string _connectionStatusColor = "Green";

    [ObservableProperty]
    private bool _isTesting;

    public DbSettingsViewModel(IDbConnectionService dbConnectionService, IAppSettings appSettings)
    {
        _dbConnectionService = dbConnectionService;
        _appSettings = appSettings;
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var connectionString = _dbConnectionService.GetCurrentConnectionString();
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);

        Server = builder.DataSource;
        Database = builder.InitialCatalog;
        UseWindowsAuth = builder.IntegratedSecurity;
        TrustServerCertificate = builder.TrustServerCertificate;
        AutoLoginEnabled = _appSettings.AutoLoginEnabled;

        if (!UseWindowsAuth)
        {
            DbUsername = builder.UserID;
            DbPassword = builder.Password;
        }
    }

    private string BuildConnectionString()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = UseWindowsAuth,
            TrustServerCertificate = TrustServerCertificate
        };

        if (!UseWindowsAuth)
        {
            builder.UserID = DbUsername;
            builder.Password = DbPassword;
        }

        return builder.ConnectionString;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        ConnectionStatus = "Проверка соединения...";

        try
        {
            var connectionString = BuildConnectionString();
            var success = await _dbConnectionService.TestConnectionAsync(connectionString);
            ConnectionStatus = success ? "Соединение успешно" : "Ошибка соединения";
            ConnectionStatusColor = success ? "Green" : "Red";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка: {ex.Message}";
            ConnectionStatusColor = "Red";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var connectionString = BuildConnectionString();
        _dbConnectionService.SaveConnectionString(connectionString);
        _appSettings.SaveAutoLoginEnabled(AutoLoginEnabled);
    }
}
