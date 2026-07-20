using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public interface IProjectVersionService
{
    Task<IReadOnlyList<ProjectVersionDto>> ListAsync(int projectId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto?> GetByIdAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto> CreateAsync(int projectId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto?> UpdateAsync(int projectId, int versionId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto> CopyAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        AnalysisDirection? targetDirection = null,
        CancellationToken cancellationToken = default);
}
