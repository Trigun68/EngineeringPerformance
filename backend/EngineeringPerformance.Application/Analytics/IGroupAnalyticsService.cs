namespace EngineeringPerformance.Application.Analytics;

public interface IGroupAnalyticsService
{
    Task<GroupAnalysisResultDto> GetGroupAnalysisAsync(int projectId, CancellationToken cancellationToken = default);

    Task<GroupTrendInfoDto> GetGroupTrendAsync(int projectId, CancellationToken cancellationToken = default);
}
