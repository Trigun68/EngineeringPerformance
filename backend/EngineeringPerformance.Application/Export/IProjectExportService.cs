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
}
