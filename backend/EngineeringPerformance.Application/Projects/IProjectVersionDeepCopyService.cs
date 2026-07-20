using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public interface IProjectVersionDeepCopyService
{
    Task<ProjectVersionDto> CopyVersionAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        AnalysisDirection? targetDirection = null,
        CancellationToken cancellationToken = default);
}
