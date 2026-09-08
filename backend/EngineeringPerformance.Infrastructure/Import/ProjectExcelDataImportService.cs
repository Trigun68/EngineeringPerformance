using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService : IProjectImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<ProjectExcelDataImportService> _logger;
    private readonly IPhysicalVolumeExcelImportService _physicalVolumeExcelImport;

    private async Task<DataImportResultDto> ImportFromFileAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        string filePath,
        string description,
        ImportSourceMetadata source,
        bool addMissingLaborToCatalog = false)
    {
        if (!File.Exists(filePath))
        {
            return new DataImportResultDto(false, 0, $"File not found: {filePath}", 0);
        }

        try
        {
            return dataType switch
            {
                DataImportType.PhysicalVolume => await _physicalVolumeExcelImport.ImportPhysicalVolumesAsync(projectId, versionId, filePath, description, source),
                DataImportType.LaborIntensity => await ImportLaborIntensitiesAsync(projectId, versionId, filePath, description, source, addMissingLaborToCatalog),
                DataImportType.Equipment => await ImportEquipmentAsync(projectId, versionId, filePath, description, source),
                _ => new DataImportResultDto(false, 0, $"Unsupported import type: {dataType}", 0),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed for project {ProjectId}, version {VersionId}, type {DataType}", projectId, versionId, dataType);
            return new DataImportResultDto(false, 0, FormatImportExceptionMessage(ex), 0);
        }
    }

    private Task<IReadOnlyList<DataImportIterationInfoDto>> GetImportIterationsCoreAsync(
        int projectId, int versionId, DataImportType dataType, CancellationToken cancellationToken = default) =>
        ImportIterationRecorder.GetImportIterationsCoreAsync(
            _contextFactory, projectId, versionId, dataType, cancellationToken);

    /// <summary>
    /// Единая точка создания итерации импорта: описание, происхождение файла и автор
    /// сохраняются вместе с данными в одной транзакции.
    /// </summary>
    private static Task<DataImportIteration> CreateIterationAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        DataImportType dataType,
        string description,
        ImportSourceMetadata source) =>
        ImportIterationRecorder.CreateIterationAsync(
            context, projectId, versionId, dataType, description, source);

    /// <summary>
    /// EnableRetryOnFailure требует оборачивать пользовательские транзакции в CreateExecutionStrategy.
    /// </summary>
    private Task<T> ExecuteWithRetryableTransactionAsync<T>(
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(_contextFactory, action, cancellationToken);

    private Task<int> DeleteIterationCoreAsync(
        int projectId, int versionId, DataImportType dataType, DateTime importedAt, CancellationToken cancellationToken = default) =>
        ImportIterationRecorder.DeleteIterationCoreAsync(
            _contextFactory,
            projectId,
            versionId,
            dataType,
            importedAt,
            DeleteImportedRecordsAsync,
            cancellationToken);

    private static string FormatImportExceptionMessage(Exception ex)
    {
        // Log full exception server-side (already logged at call site).
        // Return a generic message to avoid leaking DB schema, constraint names,
        // connection details, or other internal information to the caller.
        return "Ошибка импорта данных. Обратитесь к администратору.";
    }

    /// <summary>
    /// Диспетчер удаления строк данных по типу импорта. ФО — ImportHelpers;
    /// трудоёмкость и оборудование — Delete* в соответствующих partial.
    /// </summary>
    private static async Task<int> DeleteImportedRecordsAsync(AppDbContext context, DataImportType dataType, int projectId, int versionId, DateTime importedAt)
    {
        return dataType switch
        {
            DataImportType.PhysicalVolume => await ImportHelpers.DeletePhysicalVolumeRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.LaborIntensity => await DeleteLaborIntensityRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.Equipment => await DeleteEquipmentRecordsAsync(context, projectId, versionId, importedAt),
            _ => 0,
        };
    }
}
