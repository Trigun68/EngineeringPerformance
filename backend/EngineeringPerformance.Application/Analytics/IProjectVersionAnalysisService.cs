namespace EngineeringPerformance.Application.Analytics;

public interface IProjectVersionAnalysisService
{
    Task<ProjectVersionAnalysisDto> GenerateReportAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}
