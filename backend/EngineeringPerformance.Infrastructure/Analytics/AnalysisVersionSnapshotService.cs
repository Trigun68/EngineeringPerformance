using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Фиксация снимка норм IIC на момент создания версии анализа.
/// Без снимка Z-score-отчёты «плыли» бы при последующей правке справочника норм.
/// </summary>
public static class AnalysisVersionSnapshotService
{
    /// <summary>
    /// Создаёт записи <see cref="AnalysisVersionSnapshot"/> для версии по нормам,
    /// действовавшим на <paramref name="versionCreatedAt"/>.
    /// </summary>
    public static async Task PopulateSnapshotAsync(
        AppDbContext context,
        int versionId,
        DateTime versionCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var activeNorms = await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.EffectiveFrom <= versionCreatedAt
                        && (n.EffectiveTo == null || n.EffectiveTo >= versionCreatedAt))
            .ToListAsync(cancellationToken);

        var pickedByDirection = activeNorms
            .GroupBy(n => n.WorkDirectionId)
            .Select(g => g.OrderByDescending(n => n.EffectiveFrom).First());

        foreach (var norm in pickedByDirection)
        {
            context.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = versionId,
                WorkDirectionId = norm.WorkDirectionId,
                NormId = norm.Id
            });
        }
    }

    /// <summary>
    /// Удаляет прежний снимок и заполняет заново на <paramref name="versionCreatedAt"/>.
    /// </summary>
    public static async Task RepopulateSnapshotAsync(
        AppDbContext context,
        int versionId,
        DateTime versionCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.AnalysisVersionSnapshots
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            context.AnalysisVersionSnapshots.RemoveRange(existing);

        await PopulateSnapshotAsync(context, versionId, versionCreatedAt, cancellationToken);
    }
}
