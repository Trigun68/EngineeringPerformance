namespace EngineeringPerformance.Application.Export;

public interface IProjectExportService
{
    Task<byte[]> ExportToPdfAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportToExcelAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastFacilityPdfAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastDetailPdfAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}
