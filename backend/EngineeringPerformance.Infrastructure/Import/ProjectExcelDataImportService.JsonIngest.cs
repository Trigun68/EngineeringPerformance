using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService
{
    public Task<DataImportResultDto> IngestPhysicalVolumesAsync(
        int projectId, int versionId, PhysicalVolumeIngestRequest request, CancellationToken cancellationToken = default) =>
        RunJsonIngestAsync(
            projectId,
            versionId,
            request.Mode,
            request.Items?.Count ?? 0,
            request.AllowEmptyReplace,
            request.Description,
            UserActionType.PhysicalVolumeImport,
            recalculate: true,
            (isReplace, source) => _physicalVolumeExcelImport.IngestPhysicalVolumesAsync(
                projectId, versionId, request.Items ?? [], isReplace, request.Description ?? "json-ingest", source, cancellationToken),
            cancellationToken);

    public Task<DataImportResultDto> IngestEquipmentAsync(
        int projectId, int versionId, EquipmentIngestRequest request, CancellationToken cancellationToken = default) =>
        RunJsonIngestAsync(
            projectId,
            versionId,
            request.Mode,
            request.Items?.Count ?? 0,
            request.AllowEmptyReplace,
            request.Description,
            UserActionType.EquipmentImport,
            recalculate: true,
            (isReplace, source) => _equipmentImport.IngestEquipmentAsync(
                projectId, versionId, request.Items ?? [], isReplace, request.Description ?? "json-ingest", source, cancellationToken),
            cancellationToken);

    public Task<DataImportResultDto> IngestLaborIntensitiesAsync(
        int projectId, int versionId, LaborIntensityIngestRequest request, CancellationToken cancellationToken = default) =>
        RunJsonIngestAsync(
            projectId,
            versionId,
            request.Mode,
            request.Items?.Count ?? 0,
            request.AllowEmptyReplace,
            request.Description,
            UserActionType.LaborIntensityImport,
            recalculate: false,
            (isReplace, source) => _laborIntensityImport.IngestLaborIntensitiesAsync(
                projectId,
                versionId,
                request.Items ?? [],
                isReplace,
                request.AddMissingLaborToCatalog,
                request.Description ?? "json-ingest",
                source,
                cancellationToken),
            cancellationToken);

    public Task<LaborImportPreviewDto> PreviewLaborIngestAsync(
        IReadOnlyList<string> documentTypeNames,
        CancellationToken cancellationToken = default) =>
        _laborIntensityImport.PreviewLaborIngestAsync(documentTypeNames, cancellationToken);

    /// <summary>
    /// Публичная точка JSON-ingest: права → валидация mode/empty replace → сервис (транзакция внутри)
    /// → журнал и пересчёт только после commit.
    /// </summary>
    private async Task<DataImportResultDto> RunJsonIngestAsync(
        int projectId,
        int versionId,
        string? mode,
        int itemCount,
        bool allowEmptyReplace,
        string? description,
        UserActionType actionType,
        bool recalculate,
        Func<bool, ImportSourceMetadata, Task<DataImportResultDto>> ingest,
        CancellationToken cancellationToken)
    {
        await EnsureCanImportAsync(projectId, cancellationToken);
        if (ImportHelpers.ValidateJsonIngest(mode, itemCount, allowEmptyReplace, out var isReplace) is { } error)
            return error;

        var source = ImportHelpers.CreateJsonIngestSource(_currentUser.UserId, description, itemCount);
        var result = await ingest(isReplace, source);
        if (result.Success)
        {
            await AfterPersistedAsync(
                actionType,
                result.IterationId,
                $"{description ?? "json-ingest"}; records={result.RecordsImported}",
                projectId,
                versionId,
                recalculate,
                cancellationToken);
        }

        return result;
    }
}
