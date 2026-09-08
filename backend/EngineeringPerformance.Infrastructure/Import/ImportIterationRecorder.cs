using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Создание, список и удаление итераций импорта (<c>DataImportIterations</c>).
/// Удаление строк данных — ImportHelpers.Delete*RecordsAsync (ФО/оборудование/трудоёмкость).
/// </summary>
internal static class ImportIterationRecorder
{
    /// <summary>
    /// Единая точка создания итерации импорта: описание, происхождение файла и автор
    /// сохраняются вместе с данными в одной транзакции.
    /// </summary>
    public static async Task<DataImportIteration> CreateIterationAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        DataImportType dataType,
        string description,
        ImportSourceMetadata source)
    {
        var iteration = new DataImportIteration
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            DataType = dataType,
            Description = description,
            SourceFileName = source.FileName,
            SourceFileHash = source.FileHash,
            ImportedByUserId = source.UserId,
            ImportedAt = DateTime.UtcNow,
        };

        context.DataImportIterations.Add(iteration);
        await context.SaveChangesAsync();
        return iteration;
    }

    public static async Task<IReadOnlyList<DataImportIterationInfoDto>> GetImportIterationsCoreAsync(
        IDbContextFactory<AppDbContext> contextFactory,
        int projectId,
        int versionId,
        DataImportType dataType,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.DataImportIterations
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == dataType)
            .OrderByDescending(i => i.ImportedAt)
            .Select(i => new DataImportIterationInfoDto(i.ImportedAt, i.RecordCount, i.Description, i.SourceFileName))
            .ToListAsync(cancellationToken);
    }

    public static Task<int> DeleteIterationCoreAsync(
        IDbContextFactory<AppDbContext> contextFactory,
        int projectId,
        int versionId,
        DataImportType dataType,
        DateTime importedAt,
        Func<AppDbContext, DataImportType, int, int, DateTime, Task<int>> deleteImportedRecords,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(contextFactory, async context =>
        {
            var iteration = await context.DataImportIterations
                .FirstOrDefaultAsync(i => i.ProjectId == projectId
                                       && i.ProjectAnalysisVersionId == versionId
                                       && i.DataType == dataType
                                       && i.ImportedAt == importedAt);

            if (iteration is null)
            {
                return 0;
            }

            var deletedRecords = await deleteImportedRecords(context, dataType, projectId, versionId, importedAt);

            context.DataImportIterations.Remove(iteration);
            await context.SaveChangesAsync(cancellationToken);

            return deletedRecords;
        }, cancellationToken);
}
