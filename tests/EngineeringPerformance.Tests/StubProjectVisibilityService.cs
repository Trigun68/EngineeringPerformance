using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Tests;

internal sealed class StubProjectVisibilityService : IProjectVisibilityService
{
    public Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> CanAccessProjectAsync(int userId, int projectId, AccessLevel requiredLevel, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<int>> GetAccessibleProjectIdsAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<int>>([1]);

    public Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProjectPermissionDto>>([]);

    public Task SaveProjectPermissionsAsync(int projectId, IReadOnlyList<ProjectPermissionUpsertRequest> participants, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> RemoveProjectPermissionAsync(int projectId, int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
