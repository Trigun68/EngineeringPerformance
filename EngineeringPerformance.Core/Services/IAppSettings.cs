namespace EngineeringPerformance.Core.Services;

/// <summary>
/// Persisted application settings (stored in %APPDATA%\EngineeringPerformance\settings.json).
/// </summary>
public interface IAppSettings
{
    string ConnectionString { get; }

    /// <summary>Разрешить вход по текущему Windows-пользователю без явного подтверждения. По умолчанию выключено.</summary>
    bool AutoLoginEnabled { get; }

    void SaveConnectionString(string connectionString);

    void SaveAutoLoginEnabled(bool enabled);

    /// <summary>Сбрасывает кеш и заставляет следующий доступ перечитать файл.</summary>
    void Reload();
}
