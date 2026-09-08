using System.Security.Cryptography;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Dictionaries;
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
    private readonly IDictionaryService _dictionaryService;
    private readonly IProjectAnalyticsCache _analyticsCache;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;
    private readonly IUserActionLogger _actionLogger;

    public ProjectExcelDataImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<ProjectExcelDataImportService> logger,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IProjectVisibilityService visibility,
        IDictionaryService dictionaryService,
        IProjectAnalyticsCache analyticsCache,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
        IUserActionLogger actionLogger,
        IPhysicalVolumeExcelImportService physicalVolumeExcelImport)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _visibility = visibility;
        _dictionaryService = dictionaryService;
        _analyticsCache = analyticsCache;
        _recalcOrchestrator = recalcOrchestrator;
        _actionLogger = actionLogger;
        _physicalVolumeExcelImport = physicalVolumeExcelImport;
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
        // GAP WPF: проверка прав ProjectDataUpload до начала импорта (defense-in-depth к [HasPermission]).
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _permissionService.CanEditAsync(userId, PermissionType.ProjectDataUpload, cancellationToken))
            throw new UnauthorizedAccessException("User cannot upload project data.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Edit, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot edit project {projectId}.");

        var tempPath = await SaveTempFileAsync(fileStream, fileName, cancellationToken);
        try
        {
            var size = contentLength ?? new FileInfo(tempPath).Length;
            if (!ImportUploadValidator.TryValidate(fileName, size, contentType, out var validationError))
                return new DataImportResultDto(false, 0, validationError, 0);
            var source = new ImportSourceMetadata(
                Path.GetFileName(fileName),
                ComputeFileSha256Hex(tempPath),
                userId);

            var result = await ImportFromFileAsync(
                projectId,
                versionId,
                dataType,
                tempPath,
                description ?? fileName,
                source,
                addMissingLaborToCatalog);

            if (result.Success)
            {
                await LogImportPersistedAsync(
                    ToImportActionType(dataType),
                    result.IterationId,
                    $"{Path.GetFileName(fileName)}; records={result.RecordsImported}",
                    cancellationToken);

                if (dataType is DataImportType.PhysicalVolume or DataImportType.Equipment)
                    await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);
                else
                    await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);
            }

            return result;
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    public async Task<LaborImportPreviewDto> PreviewLaborImportAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var tempPath = await SaveTempFileAsync(fileStream, "preview.xlsx", cancellationToken);
        try
        {
            return await PreviewLaborImportAsync(tempPath);
        }
        finally
        {
            TryDeleteFile(tempPath);
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

            if (dataType is DataImportType.PhysicalVolume or DataImportType.Equipment)
                await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);
            else
                await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);
        }

        return deleted;
    }

    private static async Task<string> SaveTempFileAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".xlsx";

        var tempPath = Path.Combine(Path.GetTempPath(), $"ep-import-{Guid.NewGuid():N}{ext}");
        await using var fs = File.Create(tempPath);
        if (stream.CanSeek)
            stream.Position = 0;
        await stream.CopyToAsync(fs, cancellationToken);
        return tempPath;
    }

    private static string ComputeFileSha256Hex(string filePath)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(filePath);
        return Convert.ToHexString(sha.ComputeHash(fs));
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Временный файл — не критично при сбое удаления.
        }
    }

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
