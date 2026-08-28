namespace EngineeringPerformance.Application.Analytics;

/// <summary>Excel/PDF поверхностей «Тренд»: имена через ExportFileNameBuilder (отчет_тренд / аналитика_тренд).</summary>
public interface ITrendExportService
{
    Task<byte[]> ExportProjectExcelAsync(int projectId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportProjectPdfAsync(int projectId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAnalyticsExcelAsync(ZTrendRequest request, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAnalyticsPdfAsync(ZTrendRequest request, CancellationToken cancellationToken = default);
}
