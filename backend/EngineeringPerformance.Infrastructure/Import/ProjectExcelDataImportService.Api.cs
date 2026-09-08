using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly IProjectVisibilityService _visibility;
    private readonly IProjectAnalyticsCache _analyticsCache;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;
    private readonly IUserActionLogger _actionLogger;

    public ProjectExcelDataImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<ProjectExcelDataImportService> logger,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IProjectVisibilityService visibility,
        IProjectAnalyticsCache analyticsCache,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
        IUserActionLogger actionLogger,
        IPhysicalVolumeExcelImportService physicalVolumeExcelImport,
        IEquipmentImportService equipmentImport,
        ILaborIntensityImportService laborIntensityImport)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _visibility = visibility;
        _analyticsCache = analyticsCache;
        _recalcOrchestrator = recalcOrchestrator;
        _actionLogger = actionLogger;
        _physicalVolumeExcelImport = physicalVolumeExcelImport;
        _equipmentImport = equipmentImport;
        _laborIntensityImport = laborIntensityImport;
    }

    public async Task<DataImportResultDto> ImportProjectDataAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        Stream fileStream,
        string fileName,
        string? description,
        bool addMissingLaborToCatalog = false,
        string? contentType = null,
        long? contentLength = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(projectId, cancellationToken);

        var tempPath = await ImportHelpers.SaveTempFileAsync(fileStream, fileName, cancellationToken);
        try
        {
            var size = contentLength ?? new FileInfo(tempPath).Length;
            if (!ImportUploadValidator.TryValidate(fileName, size, contentType, out var validationError))
                return new DataImportResultDto(false, 0, validationError, 0);
            var source = new ImportSourceMetadata(
                Path.GetFileName(fileName),
                ImportHelpers.ComputeFileSha256Hex(tempPath),
                _currentUser.UserId);

            var result = await ImportFromFileAsync(
                projectId, versionId, dataType, tempPath, description ?? fileName, source, addMissingLaborToCatalog);

            if (result.Success)
            {
                await AfterPersistedAsync(
                    ToImportActionType(dataType),
                    result.IterationId,
                    $"{Path.GetFileName(fileName)}; records={result.RecordsImported}",
                    projectId,
                    versionId,
                    dataType is DataImportType.PhysicalVolume or DataImportType.Equipment,
                    cancellationToken);
            }

            return result;
        }
        finally
        {
            ImportHelpers.TryDeleteFile(tempPath);
        }
    }

    public async Task<LaborImportPreviewDto> PreviewLaborImportAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var tempPath = await ImportHelpers.SaveTempFileAsync(fileStream, "preview.xlsx", cancellationToken);
        try
        {
            return await _laborIntensityImport.PreviewLaborImportAsync(tempPath);
        }
        finally
        {
            ImportHelpers.TryDeleteFile(tempPath);
        }
    }

    public Task<IReadOnlyList<DataImportIterationInfoDto>> GetImportIterationsAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        CancellationToken cancellationToken = default) =>
        GetImportIterationsCoreAsync(projectId, versionId, dataType, cancellationToken);

    public async Task<int> DeleteIterationAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        DateTime importedAt,
        CancellationToken cancellationToken = default)
    {
        var deleted = await DeleteIterationCoreAsync(projectId, versionId, dataType, importedAt, cancellationToken);
        if (deleted > 0)
        {
            await _actionLogger.LogAsync(
                UserActionType.ImportRollback,
                entityType: "DataImportIteration",
                entityId: versionId,
                description: $"{dataType}; importedAt={importedAt:O}; deleted={deleted}",
                cancellationToken: cancellationToken);
            await AfterRecalcOrInvalidateAsync(
                projectId, versionId, dataType is DataImportType.PhysicalVolume or DataImportType.Equipment, cancellationToken);
        }

        return deleted;
    }

    private async Task EnsureCanImportAsync(int projectId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _permissionService.CanEditAsync(userId, PermissionType.ProjectDataUpload, cancellationToken))
            throw new UnauthorizedAccessException("User cannot upload project data.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Edit, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot edit project {projectId}.");
    }

    private async Task AfterPersistedAsync(
        UserActionType actionType,
        int? entityId,
        string description,
        int projectId,
        int versionId,
        bool recalculate,
        CancellationToken cancellationToken)
    {
        await LogImportPersistedAsync(actionType, entityId, description, cancellationToken);
        await AfterRecalcOrInvalidateAsync(projectId, versionId, recalculate, cancellationToken);
    }

    private Task AfterRecalcOrInvalidateAsync(
        int projectId, int versionId, bool recalculate, CancellationToken cancellationToken) =>
        recalculate
            ? _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken)
            : _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);

    private static UserActionType ToImportActionType(DataImportType dataType) =>
        dataType switch
        {
            DataImportType.PhysicalVolume => UserActionType.PhysicalVolumeImport,
            DataImportType.LaborIntensity => UserActionType.LaborIntensityImport,
            DataImportType.Equipment => UserActionType.EquipmentImport,
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null),
        };

    /// <summary>Журнал только после успешного persist; предпросмотры не пишем.</summary>
    private Task LogImportPersistedAsync(
        UserActionType actionType,
        int? entityId,
        string description,
        CancellationToken cancellationToken) =>
        _actionLogger.LogAsync(
            actionType,
            entityType: "DataImportIteration",
            entityId: entityId is > 0 ? entityId : null,
            description: description,
            cancellationToken: cancellationToken);
}
