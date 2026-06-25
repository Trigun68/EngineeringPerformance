namespace EngineeringPerformance.Application.Analytics;

public interface IGroupAnalyticsService
{
    Task<GroupAnalyticsPlaceholderDto> GetGroupAnalysisAsync(CancellationToken cancellationToken = default);

    Task<GroupAnalyticsPlaceholderDto> GetGroupTrendAsync(CancellationToken cancellationToken = default);
}
