namespace EngineeringPerformance.Application.Analytics;

public interface IProjectCompareService
{
    Task<VersionCompareResultDto> CompareAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default);
}
