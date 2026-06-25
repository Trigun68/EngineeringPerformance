using EngineeringPerformance.Application.Projects.DTOs;

namespace EngineeringPerformance.Application.Projects.Queries;

public sealed record GetProjectByIdQuery(int Id);

public sealed class GetProjectByIdQueryHandler
{
    private readonly IProjectRepository _repository;

    public GetProjectByIdQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public Task<ProjectDto?> HandleAsync(GetProjectByIdQuery query, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(query.Id, cancellationToken);
}
