using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectVisibilityService : IProjectVisibilityService
{
    private readonly AppDbContext _context;

    public ProjectVisibilityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Role.Name == nameof(UserRole.Administrator))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanAccessProjectAsync(int userId, int projectId, AccessLevel requiredLevel, CancellationToken cancellationToken = default)
    {
        if (await IsAdministratorAsync(userId, cancellationToken))
            return true;

        var projectActive = await _context.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == projectId && !p.IsArchived, cancellationToken);
        if (!projectActive)
            return false;

        var permission = await _context.ProjectPermissions.AsNoTracking()
            .FirstOrDefaultAsync(pp => pp.ProjectId == projectId && pp.UserId == userId, cancellationToken);

        return permission is not null && permission.Level >= requiredLevel;
    }

    public async Task<IReadOnlyList<int>> GetAccessibleProjectIdsAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdministratorAsync(userId, cancellationToken))
        {
            return await _context.Projects.AsNoTracking()
                .Where(p => !p.IsArchived)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        return await _context.ProjectPermissions.AsNoTracking()
            .Where(pp => pp.UserId == userId)
            .Join(_context.Projects.AsNoTracking().Where(p => !p.IsArchived),
                pp => pp.ProjectId,
                p => p.Id,
                (pp, _) => pp.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectPermissions.AsNoTracking()
            .Where(pp => pp.ProjectId == projectId)
            .Select(pp => new ProjectPermissionDto(
                pp.UserId,
                pp.User.Username,
                pp.User.DisplayName,
                pp.User.Role.Name,
                pp.Level))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveProjectPermissionsAsync(int projectId, IReadOnlyList<ProjectPermissionUpsertRequest> participants, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProjectPermissions
            .Where(pp => pp.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var incomingUserIds = participants.Select(p => p.UserId).ToHashSet();

        foreach (var row in existing.Where(e => !incomingUserIds.Contains(e.UserId)))
            _context.ProjectPermissions.Remove(row);

        foreach (var participant in participants)
        {
            var row = existing.FirstOrDefault(e => e.UserId == participant.UserId);
            if (row is null)
            {
                _context.ProjectPermissions.Add(new ProjectPermission
                {
                    ProjectId = projectId,
                    UserId = participant.UserId,
                    Level = participant.Level,
                });
            }
            else
            {
                row.Level = participant.Level;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveProjectPermissionAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.ProjectPermissions
            .FirstOrDefaultAsync(pp => pp.ProjectId == projectId && pp.UserId == userId, cancellationToken);
        if (row is null)
            return false;

        _context.ProjectPermissions.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
