using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// V2-C: реализация на базе <see cref="ProjectPermission"/>. Семантика —
/// «нет записи → нет доступа», кроме Administrator (он видит всё).
/// Прежняя <c>Project.Visibility</c> + <c>ProjectUserAccess</c> удалена.
/// </summary>
public sealed class ProjectVisibilityService : IProjectVisibilityService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectVisibilityService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> CanUserAccessProjectAsync(UserDto user, int projectId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await IsAdministratorAsync(context, user.RoleId))
            return true;

        var projectActive = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && !p.IsArchived);
        if (!projectActive)
            return false;

        return await context.ProjectPermissions
            .AsNoTracking()
            .AnyAsync(pp => pp.ProjectId == projectId && pp.UserId == user.Id);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAccessibleProjectsAsync(UserDto user)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var isAdmin = await IsAdministratorAsync(context, user.RoleId);

        var query = context.Projects
            .AsNoTracking()
            .Where(p => !p.IsArchived);

        if (!isAdmin)
        {
            query = query.Where(p =>
                context.ProjectPermissions.Any(pp =>
                    pp.ProjectId == p.Id && pp.UserId == user.Id));
        }

        return await query
            .Select(p => new ProjectDto(
                p.Id,
                p.Code,
                p.Name,
                p.WorkNumber,
                p.ContractNumber,
                p.CalendarPlanNumber,
                p.Type,
                p.Customer,
                p.Contractor,
                p.Gip,
                p.WorkStatus,
                p.WorkStart,
                p.Program,
                p.AnalysisVersions.Count))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetProjectUserIdsAsync(int projectId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectPermissions
            .AsNoTracking()
            .Where(pp => pp.ProjectId == projectId)
            .Select(pp => pp.UserId)
            .ToListAsync();
    }

    public async Task AddUserToProjectAsync(int projectId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var exists = await context.ProjectPermissions
            .AnyAsync(pp => pp.ProjectId == projectId && pp.UserId == userId);

        if (!exists)
        {
            context.ProjectPermissions.Add(new ProjectPermission
            {
                ProjectId = projectId,
                UserId = userId,
                Level = AccessLevel.Read
            });
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveUserFromProjectAsync(int projectId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var permission = await context.ProjectPermissions
            .FirstOrDefaultAsync(pp => pp.ProjectId == projectId && pp.UserId == userId);

        if (permission is not null)
        {
            context.ProjectPermissions.Remove(permission);
            await context.SaveChangesAsync();
        }
    }

    public async Task AddOrUpdateUserPermissionAsync(int projectId, int userId, AccessLevel level)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ProjectPermissions
            .FirstOrDefaultAsync(pp => pp.ProjectId == projectId && pp.UserId == userId);

        if (existing is null)
        {
            context.ProjectPermissions.Add(new ProjectPermission
            {
                ProjectId = projectId,
                UserId = userId,
                Level = level
            });
        }
        else if (existing.Level != level)
        {
            existing.Level = level;
        }
        else
        {
            // Идемпотентный no-op: уровень уже совпадает, лишний UPDATE не нужен.
            return;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectPermissions
            .AsNoTracking()
            .Where(pp => pp.ProjectId == projectId)
            .Select(pp => new ProjectPermissionDto(
                pp.UserId,
                pp.User.Username,
                pp.User.DisplayName,
                pp.User.Role.Name,
                pp.Level))
            .ToListAsync();
    }

    private static async Task<bool> IsAdministratorAsync(AppDbContext context, int roleId)
    {
        return await context.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId)
            .Select(r => r.Name == nameof(UserRole.Administrator))
            .FirstOrDefaultAsync();
    }
}
