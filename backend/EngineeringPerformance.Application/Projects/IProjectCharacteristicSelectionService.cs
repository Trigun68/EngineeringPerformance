namespace EngineeringPerformance.Application.Projects;

public interface IProjectCharacteristicSelectionService
{
    Task<IReadOnlyList<ProjectCharacteristicSelectionDto>> GetByProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task SaveByProjectAsync(
        int projectId,
        IReadOnlyList<ProjectCharacteristicSelectionWriteDto> selections,
        CancellationToken cancellationToken = default);
}
