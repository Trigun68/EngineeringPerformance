using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Заглушка групповой аналитики — WPF-формы GroupAnalysis/GroupTrend пока пустые.
/// </summary>
public sealed class GroupAnalyticsService : IGroupAnalyticsService
{
    private const string GroupAnalysisMessage =
        "Форма анализа группы проектов / направлений работ IIC — в разработке.";

    private const string GroupTrendMessage =
        "Форма тренда группы проектов — в разработке.";

    public Task<GroupAnalyticsPlaceholderDto> GetGroupAnalysisAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new GroupAnalyticsPlaceholderDto(GroupAnalysisMessage));

    public Task<GroupAnalyticsPlaceholderDto> GetGroupTrendAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new GroupAnalyticsPlaceholderDto(GroupTrendMessage));
}
