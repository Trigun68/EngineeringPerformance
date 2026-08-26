namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводка «план/факт» проекта по основным версиям (см. <see cref="ProjectReportSummaryDto"/>).
/// </summary>
public interface IProjectReportSummaryService
{
    Task<ProjectReportSummaryDto> GetSummaryAsync(int projectId, CancellationToken cancellationToken = default);
}
