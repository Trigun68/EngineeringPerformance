using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Replace-стратегия импорта оборудования: upsert через EF Core (без raw SQL / MERGE), skip ошибок без rollback.
/// </summary>
internal sealed class EquipmentImportReplaceProcessor
{
    public sealed record RowInput(
        int RowNumber,
        int? ExistingRowId,
        ProjectEquipment Draft,
        bool IsValid);

    public sealed record Result(
        int ProcessedCount,
        IReadOnlyList<ImportRowErrorDto> Errors);

    public static async Task<Result> ApplyAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        IReadOnlyList<RowInput> rows,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ImportRowErrorDto>();
        var processed = 0;

        var existingRows = await context.ProjectEquipments
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var byId = existingRows.ToDictionary(e => e.Id);
        var byNaturalKey = existingRows
            .GroupBy(EquipmentImportNaturalKey.FromEntity)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in rows)
        {
            if (!row.IsValid)
            {
                errors.Add(new ImportRowErrorDto(row.RowNumber, "Invalid row format or missing required catalog name."));
                continue;
            }

            try
            {
                var naturalKey = EquipmentImportNaturalKey.FromEntity(row.Draft);

                ProjectEquipment? existing = null;
                if (row.ExistingRowId is int rowId)
                    byId.TryGetValue(rowId, out existing);
                else
                    byNaturalKey.TryGetValue(naturalKey, out existing);

                if (existing is null)
                    byNaturalKey.TryGetValue(naturalKey, out existing);

                if (existing is not null)
                {
                    // REPLACE: стандартный EF Core upsert — SetValues, без MERGE / INSERT OR REPLACE.
                    ApplyDraftToExisting(context, existing, row.Draft, projectId, versionId);
                    processed++;
                    continue;
                }

                row.Draft.ProjectId = projectId;
                row.Draft.ProjectAnalysisVersionId = versionId;
                context.ProjectEquipments.Add(row.Draft);
                byNaturalKey[naturalKey] = row.Draft;
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Equipment import row {RowNumber} skipped", row.RowNumber);
                errors.Add(new ImportRowErrorDto(row.RowNumber, ex.Message));
            }
        }

        return new Result(processed, errors);
    }

    private static void ApplyDraftToExisting(
        AppDbContext context,
        ProjectEquipment existing,
        ProjectEquipment draft,
        int projectId,
        int versionId)
    {
        var preservedId = existing.Id;
        context.Entry(existing).CurrentValues.SetValues(draft);
        existing.Id = preservedId;
        existing.ProjectId = projectId;
        existing.ProjectAnalysisVersionId = versionId;
    }
}
