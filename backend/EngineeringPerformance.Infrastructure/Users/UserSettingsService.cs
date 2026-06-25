using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Users;

public sealed class UserSettingsService : IUserSettingsService
{
    private const int MaxValueLength = 64 * 1024;
    private readonly AppDbContext _context;
    private readonly IUserActionLogger _actionLogger;

    public UserSettingsService(AppDbContext context, IUserActionLogger actionLogger)
    {
        _context = context;
        _actionLogger = actionLogger;
    }

    public async Task<string?> GetAsync(int userId, string key, CancellationToken cancellationToken = default)
    {
        return await _context.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId && s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task PutAsync(int userId, string key, string value, CancellationToken cancellationToken = default)
    {
        if (value.Length > MaxValueLength)
            throw new InvalidOperationException($"Setting value exceeds {MaxValueLength} bytes.");

        var existing = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key, cancellationToken);

        if (existing is null)
        {
            _context.UserSettings.Add(new UserSetting { UserId = userId, Key = key, Value = value });
        }
        else
        {
            existing.Value = value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.UserSettingUpdate,
            entityType: "UserSetting",
            entityId: userId,
            description: $"Setting '{key}' updated for user {userId}");
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _context.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

    public async Task PutManyAsync(
        int userId,
        IReadOnlyDictionary<string, string> preferences,
        CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in preferences)
            await PutAsync(userId, key, value, cancellationToken);
    }
}
