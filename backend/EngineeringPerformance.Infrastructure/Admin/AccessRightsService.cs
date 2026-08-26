using EngineeringPerformance.Application.Admin;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Admin;

public sealed class AccessRightsService : IAccessRightsService
{
    private readonly AppDbContext _context;

    public AccessRightsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AccessRightDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.AccessRights
            .AsNoTracking()
            .Include(r => r.Role)
            .OrderBy(r => r.RoleId)
            .ThenBy(r => r.ObjectName)
            .Select(r => new AccessRightDto(r.Id, r.RoleId, r.Role.Name, r.ObjectName, r.AccessLevel))
            .ToListAsync(cancellationToken);

    public async Task<bool> UpdateAsync(int id, AccessLevel accessLevel, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AccessRights.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
            return false;

        entity.AccessLevel = accessLevel;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default)
    {
        var objects = Enum.GetValues<PermissionType>()
            .Select(p => p.ToObjectName())
            .OrderBy(name => name)
            .ToList();

        var roles = await _context.Roles
            .AsNoTracking()
            .Where(r => !r.IsArchived)
            .OrderBy(r => r.Id)
            .Select(r => new PermissionMatrixRoleDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);

        var rights = await _context.AccessRights
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rightsLookup = rights.ToDictionary(
            r => (r.ObjectName, r.RoleId),
            r => r.AccessLevel);

        var matrix = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var objectName in objects)
        {
            var roleLevels = new Dictionary<string, string>();
            foreach (var role in roles)
            {
                var level = rightsLookup.TryGetValue((objectName, role.Id), out var accessLevel)
                    ? accessLevel
                    : AccessLevel.None;
                roleLevels[role.Id.ToString()] = ToMatrixLevel(level);
            }

            matrix[objectName] = roleLevels;
        }

        var hierarchy = PermissionMatrixHierarchy.Build()
            .Select(node => new PermissionMatrixObjectDto(
                node.ObjectName,
                node.DisplayName,
                node.ParentGroup,
                node.Level,
                node.HierarchyPath,
                node.IsGroupHeader))
            .ToList();

        return new PermissionMatrixDto(objects, hierarchy, roles, matrix);
    }

    public async Task<(bool Success, string? Error)> UpdateMatrixAsync(
        PermissionMatrixUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validObjects = Enum.GetValues<PermissionType>()
            .Select(p => p.ToObjectName())
            .ToHashSet(StringComparer.Ordinal);

        var roles = await _context.Roles
            .Where(r => !r.IsArchived)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToHashSet();
        var administratorRoleIds = roles
            .Where(r => PermissionObjectCatalog.IsSuperAdmin(r.Name, r.Id))
            .Select(r => r.Id)
            .ToHashSet();

        foreach (var (objectName, roleLevels) in request.Matrix)
        {
            if (!validObjects.Contains(objectName))
                return (false, $"Неизвестный объект доступа: {objectName}");

            foreach (var (roleIdRaw, levelRaw) in roleLevels)
            {
                if (!int.TryParse(roleIdRaw, out var roleId) || !roleIds.Contains(roleId))
                    return (false, $"Неизвестная роль: {roleIdRaw}");

                if (administratorRoleIds.Contains(roleId))
                    continue;

                if (!TryParseMatrixLevel(levelRaw, out var accessLevel))
                    return (false, $"Недопустимый уровень доступа: {levelRaw}");

                var entity = await _context.AccessRights
                    .FirstOrDefaultAsync(r => r.RoleId == roleId && r.ObjectName == objectName, cancellationToken);

                if (entity is null)
                {
                    _context.AccessRights.Add(new Domain.Entities.AccessRight
                    {
                        RoleId = roleId,
                        ObjectName = objectName,
                        AccessLevel = accessLevel,
                    });
                }
                else
                {
                    entity.AccessLevel = accessLevel;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private static string ToMatrixLevel(AccessLevel level) => level switch
    {
        AccessLevel.Edit => "Edit",
        AccessLevel.Read => "Read",
        _ => "None",
    };

    private static bool TryParseMatrixLevel(string raw, out AccessLevel level)
    {
        level = AccessLevel.None;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim())
        {
            case "None":
                level = AccessLevel.None;
                return true;
            case "Read":
                level = AccessLevel.Read;
                return true;
            case "Edit":
                level = AccessLevel.Edit;
                return true;
            default:
                return false;
        }
    }
}
