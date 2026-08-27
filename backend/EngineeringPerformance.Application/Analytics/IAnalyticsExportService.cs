namespace EngineeringPerformance.Application.Analytics;

public interface IAnalyticsExportService
{
    Task<byte[]> ExportAnalysisExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportProjectReportPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
}
