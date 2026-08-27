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

    Task<byte[]> ExportForecastSummaryExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactSummaryExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCompareDetailExcelAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCompareDetailSummaryTableAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default);
}
