using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Projects.DTOs;

namespace EngineeringPerformance.Application.Projects.Queries;

public sealed record GetProjectsQuery;

public sealed class GetProjectsQueryHandler
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;

    public GetProjectsQueryHandler(
        IProjectRepository repository,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility)
    {
        _repository = repository;
        _currentUser = currentUser;
        _visibility = visibility;
    }

    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int userId)
            return [];

        var isAdmin = await _visibility.IsAdministratorAsync(userId, cancellationToken);
        var accessibleIds = await _visibility.GetAccessibleProjectIdsAsync(userId, cancellationToken);
        return await _repository.GetAllForUserAsync(userId, accessibleIds, isAdmin, cancellationToken);
    }
}
