namespace EngineeringPerformance.Application.Projects;

public sealed record ProjectVersionReadinessDto(
    int ProjectId,
    int VersionId,
    bool HasPhysicalVolumeType,
    int PlanningParametersCount,
    int PhysicalVolumesCount,
    int EquipmentCount,
    int LaborIntensitiesCount,
    bool HasNormSnapshots,
    IReadOnlyList<ProjectVersionReadinessIssueDto> Issues);

public sealed record ProjectVersionReadinessIssueDto(
    string Code,
    string Severity,
    string Message);

public interface IProjectVersionReadinessService
{
    Task<ProjectVersionReadinessDto?> GetAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}
