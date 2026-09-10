using EngineeringPerformance.Application.Import;
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
    private readonly IEquipmentImportService _equipmentImport;
    private readonly ILaborIntensityImportService _laborIntensityImport;

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
            return new DataImportResultDto(false, 0, $"File not found: {filePath}", 0);

        try
        {
            return dataType switch
            {
                DataImportType.PhysicalVolume => await _physicalVolumeExcelImport.ImportPhysicalVolumesAsync(projectId, versionId, filePath, description, source),
                DataImportType.LaborIntensity => await _laborIntensityImport.ImportLaborIntensitiesAsync(projectId, versionId, filePath, description, source, addMissingLaborToCatalog),
                DataImportType.Equipment => await _equipmentImport.ImportEquipmentAsync(projectId, versionId, filePath, description, source),
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

    private static string FormatImportExceptionMessage(Exception ex) =>
        "Ошибка импорта данных. Обратитесь к администратору.";

    /// <summary>Диспетчер удаления строк данных по типу импорта.</summary>
    private static async Task<int> DeleteImportedRecordsAsync(
        AppDbContext context, DataImportType dataType, int projectId, int versionId, DateTime importedAt) =>
        dataType switch
        {
            DataImportType.PhysicalVolume => await ImportHelpers.DeletePhysicalVolumeRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.LaborIntensity => await ImportHelpers.DeleteLaborIntensityRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.Equipment => await ImportHelpers.DeleteEquipmentRecordsAsync(context, projectId, versionId, importedAt),
            _ => 0,
        };
}
