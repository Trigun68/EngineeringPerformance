using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Permissions;

/// <summary>
/// Заглушка проверки прав: читает AccessRights по роли пользователя.
/// Администратор имеет полный доступ ко всем объектам.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly Func<int, CancellationToken, Task<(int RoleId, string RoleName)?>> _getUserRole;
    private readonly Func<int, CancellationToken, Task<IReadOnlyDictionary<string, AccessLevel>>> _getAccessRights;

    public PermissionService(
        Func<int, CancellationToken, Task<(int RoleId, string RoleName)?>> getUserRole,
        Func<int, CancellationToken, Task<IReadOnlyDictionary<string, AccessLevel>>> getAccessRights)
    {
        _getUserRole = getUserRole;
        _getAccessRights = getAccessRights;
    }

    public async Task<bool> HasAccessAsync(int userId, PermissionType permission, AccessLevel requiredLevel, CancellationToken cancellationToken = default)
    {
        var role = await _getUserRole(userId, cancellationToken);
        if (role is null)
            return false;

        if (PermissionObjectCatalog.IsSuperAdmin(role.Value.RoleName, role.Value.RoleId))
            return true;

        var objectName = permission.ToObjectName();
        var rights = await _getAccessRights(role.Value.RoleId, cancellationToken);

        return rights.TryGetValue(objectName, out var level) && level >= requiredLevel;
    }

    public Task<bool> CanReadAsync(int userId, PermissionType permission, CancellationToken cancellationToken = default) =>
        HasAccessAsync(userId, permission, AccessLevel.Read, cancellationToken);

    public Task<bool> CanEditAsync(int userId, PermissionType permission, CancellationToken cancellationToken = default) =>
        HasAccessAsync(userId, permission, AccessLevel.Edit, cancellationToken);
}
