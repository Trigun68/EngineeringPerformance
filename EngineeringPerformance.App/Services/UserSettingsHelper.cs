using System.IO;
using System.Text;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.Core.Services;
using Serilog;

namespace EngineeringPerformance.App.Services;

public sealed class UserSettingsHelper
{
    private readonly IUserSettingsService _settingsService;
    private readonly Dictionary<GridControl, GridLayoutAutoSave> _gridAutoSaves = new();
    private int _userId;

    public UserSettingsHelper(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void SetUser(int userId) => _userId = userId;

    public async Task SaveGridLayoutAsync(GridControl grid, string gridName)
    {
        if (_userId <= 0 || string.IsNullOrEmpty(gridName)) return;
        using var ms = new MemoryStream();
        grid.SaveLayoutToStream(ms);
        var xml = Encoding.UTF8.GetString(ms.ToArray());
        await _settingsService.SetAsync(_userId, $"grid.{gridName}", xml);
    }

    public async Task RestoreGridLayoutAsync(GridControl grid, string gridName)
    {
        if (_userId <= 0 || string.IsNullOrEmpty(gridName)) return;
        var xml = await _settingsService.GetAsync(_userId, $"grid.{gridName}");
        if (string.IsNullOrEmpty(xml)) return;
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            grid.RestoreLayoutFromStream(ms);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to restore grid layout for {GridName}", gridName);
        }
    }

    /// <summary>Подписывает грид на изменения раскладки (ширина, порядок, видимость столбцов).</summary>
    public void AttachGridLayoutAutoSave(GridControl grid, string gridName)
    {
        if (_userId <= 0 || string.IsNullOrEmpty(gridName)) return;
        if (_gridAutoSaves.ContainsKey(grid)) return;
        _gridAutoSaves[grid] = new GridLayoutAutoSave(this, grid, gridName);
    }

    /// <summary>Снимает подписку и сохраняет текущую раскладку.</summary>
    public async Task DetachAndSaveGridLayoutAsync(GridControl grid)
    {
        if (!_gridAutoSaves.Remove(grid, out var autoSave)) return;
        await autoSave.FlushAsync();
        autoSave.Dispose();
    }

    /// <summary>Сохраняет раскладку всех подключённых гридов (перед закрытием приложения).</summary>
    public async Task FlushAllAttachedGridLayoutsAsync()
    {
        foreach (var autoSave in _gridAutoSaves.Values.ToList())
            await autoSave.FlushAsync();
    }

    /// <summary>
    /// Синхронно сохраняет раскладки всех гридов (для вызова с UI-потока при закрытии).
    /// Полностью синхронный путь — без Task.Run, без GetAwaiter().GetResult(), без риска дедлока.
    /// SQLite под капотом синхронный, поэтому SetSync не медленнее async-аналога.
    /// </summary>
    public void FlushAllSync()
    {
        if (_userId <= 0) return;

        foreach (var autoSave in _gridAutoSaves.Values)
        {
            var xml = autoSave.CollectLayoutXml();
            if (!string.IsNullOrEmpty(xml))
                _settingsService.SetSync(_userId, $"grid.{autoSave.GridName}", xml);
        }
    }

    public async Task SavePrefAsync(string key, string value)
    {
        if (_userId <= 0) return;
        await _settingsService.SetAsync(_userId, $"pref.{key}", value);
    }

    public async Task<string?> GetPrefAsync(string key)
    {
        if (_userId <= 0) return null;
        return await _settingsService.GetAsync(_userId, $"pref.{key}");
    }

    public async Task SaveBoolAsync(string key, bool value)
        => await SavePrefAsync(key, value ? "true" : "false");

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var val = await GetPrefAsync(key);
        if (val is null) return defaultValue;
        return val == "true";
    }

    public async Task SaveIntAsync(string key, int value)
        => await SavePrefAsync(key, value.ToString());

    public async Task<int?> GetIntAsync(string key)
    {
        var val = await GetPrefAsync(key);
        return int.TryParse(val, out var i) ? i : null;
    }
}
