using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public interface IProjectVisibilityService
{
    Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessProjectAsync(int userId, int projectId, AccessLevel requiredLevel, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetAccessibleProjectIdsAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default);

    Task SaveProjectPermissionsAsync(int projectId, IReadOnlyList<ProjectPermissionUpsertRequest> participants, CancellationToken cancellationToken = default);

    Task<bool> RemoveProjectPermissionAsync(int projectId, int userId, CancellationToken cancellationToken = default);
}
