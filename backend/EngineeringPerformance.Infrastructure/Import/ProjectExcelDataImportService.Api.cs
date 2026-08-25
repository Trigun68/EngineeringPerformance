using System.Security.Cryptography;
using EngineeringPerformance.Application.Analytics;
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

    public ProjectExcelDataImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<ProjectExcelDataImportService> logger,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IProjectVisibilityService visibility,
        IDictionaryService dictionaryService,
        IProjectAnalyticsCache analyticsCache,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _visibility = visibility;
        _dictionaryService = dictionaryService;
        _analyticsCache = analyticsCache;
        _recalcOrchestrator = recalcOrchestrator;
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
}
