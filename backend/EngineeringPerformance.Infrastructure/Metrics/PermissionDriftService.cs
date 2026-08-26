using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Metrics;

public sealed class PermissionDriftService : IPermissionDriftService
{
    private readonly AppDbContext _context;

    public PermissionDriftService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PermissionDriftDto> GetPermissionDriftAsync(CancellationToken cancellationToken = default)
    {
        var catalogObjects = PermissionObjectCatalog.AllObjectNames
            .ToHashSet(StringComparer.Ordinal);
        var expectedInDatabase = PermissionObjectCatalog.MatrixObjectNames
            .ToHashSet(StringComparer.Ordinal);

        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var rights = await _context.AccessRights
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var unknownObjects = rights
            .Select(r => r.ObjectName)
            .Where(name => !catalogObjects.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var missingByRole = roles
            .Where(role => !string.Equals(role.Name, PermissionObjectCatalog.SuperAdminRoleName, StringComparison.Ordinal))
            .Select(role =>
            {
                var grantedObjects = rights
                    .Where(r => r.RoleId == role.Id)
                    .Select(r => r.ObjectName)
                    .ToHashSet(StringComparer.Ordinal);

                var missing = expectedInDatabase
                    .Where(expected => !grantedObjects.Contains(expected))
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                return new RolePermissionGapDto(role.Id, role.Name, missing);
            })
            .Where(x => x.MissingObjects.Count > 0)
            .ToList();

        return new PermissionDriftDto(
            HasDrift: unknownObjects.Count > 0 || missingByRole.Count > 0,
            UnknownObjects: unknownObjects,
            MissingObjectsByRole: missingByRole,
            CheckedAtUtc: DateTime.UtcNow);
    }
}
