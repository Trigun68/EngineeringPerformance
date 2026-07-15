namespace EngineeringPerformance.Application.Analytics;

public interface IProjectCompareService
{
    Task<VersionCompareResultDto> CompareAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default);

    Task<CompareChartResultDto> GetChartAsync(
        int projectId,
        int versionAId,
        int versionBId,
        string level = "iic",
        string metric = "zScore",
        int top = 20,
        CancellationToken cancellationToken = default);
}
