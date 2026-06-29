using EngineeringPerformance.Core;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class AccessControlService : IAccessControlService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<AccessControlService> _logger;
    private readonly Dictionary<int, IReadOnlyList<AccessRight>> _rightsCache = [];
    private readonly object _cacheLock = new();

    public AccessControlService(IDbContextFactory<AppDbContext> contextFactory, ILogger<AccessControlService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public bool HasAccess(UserDto user, string objectName, AccessLevel requiredLevel)
    {
        if (IsAdministrator(user))
            return true;

        var accessLevel = GetEffectiveAccessLevel(user, objectName);
        return accessLevel >= requiredLevel;
    }

    public bool CanRead(UserDto user, string objectName)
    {
        return HasAccess(user, objectName, AccessLevel.Read);
    }

    public bool CanEdit(UserDto user, string objectName)
    {
        return HasAccess(user, objectName, AccessLevel.Edit);
    }

    public IReadOnlyList<string> GetAccessibleObjects(UserDto user, AccessLevel level)
    {
        if (IsAdministrator(user))
            return GetAllObjectNames();

        var rights = GetAccessRightsForRole(user.RoleId);
        return rights
            .Where(r => r.AccessLevel >= level)
            .Select(r => r.ObjectName)
            .ToList();
    }

    public async Task<IReadOnlyList<AccessRightDto>> GetAllAccessRightsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AccessRights
            .AsNoTracking()
            .Include(r => r.Role)
            .Select(r => new AccessRightDto(r.Id, r.RoleId, r.Role.Name, r.ObjectName, r.AccessLevel))
            .ToListAsync();
    }

    public async Task UpdateAccessRightAsync(AccessRightDto right)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.AccessRights.FindAsync(right.Id);
        if (entity is not null)
        {
            entity.AccessLevel = right.AccessLevel;
            await context.SaveChangesAsync();
            ClearCache();
        }
    }

    public async Task DeleteAccessRightAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.AccessRights.FindAsync(id);
        if (entity is not null)
        {
            context.AccessRights.Remove(entity);
            await context.SaveChangesAsync();
            ClearCache();
        }
    }

    private AccessLevel GetEffectiveAccessLevel(UserDto user, string objectName)
    {
        if (IsAdministrator(user))
            return AccessLevel.Edit;

        var rights = GetAccessRightsForRole(user.RoleId);
        var right = rights.FirstOrDefault(r => r.ObjectName == objectName);
        return right?.AccessLevel ?? AccessLevel.None;
    }

    private IReadOnlyList<AccessRight> GetAccessRightsForRole(int roleId)
    {
        lock (_cacheLock)
        {
            if (_rightsCache.TryGetValue(roleId, out var cached))
                return cached;
        }

        try
        {
            using var context = _contextFactory.CreateDbContext();
            var rights = context.AccessRights
                .AsNoTracking()
                .Where(r => r.RoleId == roleId)
                .ToList();

            lock (_cacheLock)
            {
                _rightsCache[roleId] = rights;
            }

            return rights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load access rights for role {RoleId}", roleId);
            return [];
        }
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _rightsCache.Clear();
        }
    }

    private static bool IsAdministrator(UserDto user)
    {
        return user.RoleName == nameof(UserRole.Administrator);
    }

    private static IReadOnlyList<string> GetAllObjectNames()
    {
        return AccessControlConstants.AllObjectNames;
    }
}
