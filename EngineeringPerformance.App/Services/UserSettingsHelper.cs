using System.IO;
using System.Text;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.Services;

public sealed class UserSettingsHelper
{
    private readonly IUserSettingsService _settingsService;
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
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        grid.RestoreLayoutFromStream(ms);
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
