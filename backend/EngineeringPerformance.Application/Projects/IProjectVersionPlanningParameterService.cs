namespace EngineeringPerformance.Application.Projects;

public interface IProjectVersionPlanningParameterService
{
    Task<IReadOnlyList<ProjectVersionPlanningParameterDto>> ListAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        int projectId,
        int versionId,
        SaveProjectVersionPlanningParametersRequest request,
        CancellationToken cancellationToken = default);

    Task SeedForVersionAsync(int versionId, CancellationToken cancellationToken = default);

    Task CopyFromVersionAsync(int sourceVersionId, int targetVersionId, CancellationToken cancellationToken = default);
}
