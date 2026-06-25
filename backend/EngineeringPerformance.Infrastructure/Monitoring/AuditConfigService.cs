using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Monitoring;

/// <summary>
/// Аудит изменений прав доступа и пользовательских настроек из UserActionLogs.
/// </summary>
public sealed class AuditConfigService : IAuditConfigService
{
    private static readonly string[] ConfigEntityTypes =
    [
        "AccessRight",
        "AccessRights",
        "PermissionMatrix",
        "UserSetting",
        "UserPreferences",
        "UserSettings",
    ];

    private readonly AppDbContext _context;

    public AuditConfigService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ConfigAuditEntryDto>> GetConfigAuditAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int? userId = null,
        string? search = null,
        int take = 500,
        CancellationToken cancellationToken = default)
    {
        var clampedTake = Math.Clamp(take, 1, 2000);
        var query = _context.UserActionLogs
            .AsNoTracking()
            .Include(l => l.User)
            .Where(l => !l.IsArchived)
            .Where(l =>
                (l.EntityType != null && ConfigEntityTypes.Contains(l.EntityType))
                || (l.Description != null && (
                    l.Description.Contains("прав")
                    || l.Description.Contains("Permission")
                    || l.Description.Contains("matrix")
                    || l.Description.Contains("настрой")
                    || l.Description.Contains("AccessRight")))
                || l.ActionType == "PermissionMatrixUpdate"
                || l.ActionType == "UserSettingUpdate");

        if (fromUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp <= toUtc.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                (l.Description != null && l.Description.Contains(term))
                || (l.EntityType != null && l.EntityType.Contains(term))
                || l.User.DisplayName.Contains(term)
                || l.User.Username.Contains(term));
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(clampedTake)
            .Select(l => new ConfigAuditEntryDto(
                l.Id,
                l.UserId,
                l.User.DisplayName,
                l.Timestamp,
                ResolveCategory(l.EntityType, l.ActionType),
                l.EntityType,
                l.EntityId,
                l.Description))
            .ToListAsync(cancellationToken);
    }

    private static string ResolveCategory(string? entityType, string actionType)
    {
        if (entityType is "PermissionMatrix" or "AccessRight" or "AccessRights")
        {
            return "Access Rights";
        }

        if (entityType is "UserSetting" or "UserPreferences" or "UserSettings")
        {
            return "User Settings";
        }

        if (actionType is "PermissionMatrixUpdate")
        {
            return "Access Rights";
        }

        if (actionType is "UserSettingUpdate")
        {
            return "User Settings";
        }

        return "Configuration";
    }
}
