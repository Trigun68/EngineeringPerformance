using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Import;

public interface IProjectImportService
{
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

    Task<DataImportResultDto> IngestPhysicalVolumesAsync(
        int projectId,
        int versionId,
        PhysicalVolumeIngestRequest request,
        CancellationToken cancellationToken = default);

    Task<DataImportResultDto> IngestEquipmentAsync(
        int projectId,
        int versionId,
        EquipmentIngestRequest request,
        CancellationToken cancellationToken = default);

    Task<DataImportResultDto> IngestLaborIntensitiesAsync(
        int projectId,
        int versionId,
        LaborIntensityIngestRequest request,
        CancellationToken cancellationToken = default);

    Task<LaborImportPreviewDto> PreviewLaborIngestAsync(
        IReadOnlyList<string> documentTypeNames,
        CancellationToken cancellationToken = default);
}
