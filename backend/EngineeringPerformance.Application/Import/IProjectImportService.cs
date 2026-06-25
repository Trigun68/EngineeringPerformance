using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Import;

public interface IProjectImportService
{
    /// <summary>Импорт списка проектов из Excel (legacy endpoint).</summary>
    Task<int> ImportFromExcelAsync(int projectId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    Task<DataImportResultDto> ImportProjectDataAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        Stream fileStream,
        string fileName,
        string? description,
        bool addMissingLaborToCatalog = false,
        string? contentType = null,
        long? contentLength = null,
        CancellationToken cancellationToken = default);

    Task<LaborImportPreviewDto> PreviewLaborImportAsync(Stream fileStream, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataImportIterationInfoDto>> GetImportIterationsAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        CancellationToken cancellationToken = default);

    Task<int> DeleteIterationAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        DateTime importedAt,
        CancellationToken cancellationToken = default);
}
