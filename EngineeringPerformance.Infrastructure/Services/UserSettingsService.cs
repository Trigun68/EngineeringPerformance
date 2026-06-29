using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserSettingsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string?> GetAsync(int userId, string key)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var setting = await ctx.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key);
        return setting?.Value;
    }

    public async Task SetAsync(int userId, string key, string value)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var setting = await ctx.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key);

        if (setting is null)
        {
            ctx.UserSettings.Add(new UserSetting { UserId = userId, Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await ctx.SaveChangesAsync();
    }

    public void SetSync(int userId, string key, string value)
    {
        using var ctx = _contextFactory.CreateDbContext();
        var setting = ctx.UserSettings
            .FirstOrDefault(s => s.UserId == userId && s.Key == key);

        if (setting is null)
            ctx.UserSettings.Add(new UserSetting { UserId = userId, Key = key, Value = value });
        else
            setting.Value = value;

        ctx.SaveChanges();
    }

    public async Task<Dictionary<string, string>> GetAllAsync(int userId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.UserSettings
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
    }
}
