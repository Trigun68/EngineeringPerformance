using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Permissions;

public interface IPermissionService
{
    Task<bool> HasAccessAsync(int userId, PermissionType permission, AccessLevel requiredLevel, CancellationToken cancellationToken = default);

    Task<bool> CanReadAsync(int userId, PermissionType permission, CancellationToken cancellationToken = default);

    Task<bool> CanEditAsync(int userId, PermissionType permission, CancellationToken cancellationToken = default);
}
