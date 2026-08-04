namespace EngineeringPerformance.Application.Analytics;

public interface IPerformanceReportService
{
    Task<PerformanceReportDto> GetReportAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default);
}
