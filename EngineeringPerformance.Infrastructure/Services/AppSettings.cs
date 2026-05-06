using System.IO;
using System.Text.Json;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// JSON-backed реализация <see cref="IAppSettings"/>. Файл хранится в
/// <c>%APPDATA%\EngineeringPerformance\settings.json</c>. Класс потокобезопасен.
/// </summary>
public sealed class AppSettings : IAppSettings
{
    private static readonly string DefaultSettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EngineeringPerformance",
        "settings.json");

    private const string DefaultConnectionStringValue =
        "Server=.\\SQLEXPRESS;Database=EngineeringPerformance;Trusted_Connection=True;";

    private readonly string _filePath;
    private readonly object _lock = new();
    private AppSettingsData? _cached;

    public AppSettings() : this(DefaultSettingsFilePath)
    {
    }

    public AppSettings(string filePath)
    {
        _filePath = filePath;
    }

    public string ConnectionString
    {
        get
        {
            lock (_lock)
            {
                var data = Load();
                return data.ConnectionString ?? DefaultConnectionStringValue;
            }
        }
    }

    public bool AutoLoginEnabled
    {
        get
        {
            lock (_lock)
            {
                return Load().AutoLoginEnabled;
            }
        }
    }

    public void SaveConnectionString(string connectionString)
    {
        lock (_lock)
        {
            var data = Load();
            data.ConnectionString = connectionString;
            Persist(data);
        }
    }

    public void SaveAutoLoginEnabled(bool enabled)
    {
        lock (_lock)
        {
            var data = Load();
            data.AutoLoginEnabled = enabled;
            Persist(data);
        }
    }

    public void Reload()
    {
        lock (_lock) { _cached = null; }
    }

    private AppSettingsData Load()
    {
        if (_cached is not null) return _cached;

        if (!File.Exists(_filePath))
        {
            _cached = new AppSettingsData { ConnectionString = DefaultConnectionStringValue };
            return _cached;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _cached = JsonSerializer.Deserialize<AppSettingsData>(json)
                      ?? new AppSettingsData { ConnectionString = DefaultConnectionStringValue };
        }
        catch (JsonException)
        {
            _cached = new AppSettingsData { ConnectionString = DefaultConnectionStringValue };
        }

        return _cached;
    }

    private void Persist(AppSettingsData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        _cached = data;
    }

    private sealed class AppSettingsData
    {
        public string? ConnectionString { get; set; }
        public bool AutoLoginEnabled { get; set; }
    }
}
