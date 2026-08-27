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

    /// <summary>
    /// Копия наследует снимок норм источника, чтобы live N(V)×Z совпадал с оригиналом.
    /// Если у источника снимка нет — заполняем по дате новой версии.
    /// </summary>
    public static async Task CopyFromVersionAsync(
        AppDbContext context,
        int sourceVersionId,
        int targetVersionId,
        DateTime targetCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var sourceRows = await context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == sourceVersionId)
            .Select(s => new { s.WorkDirectionId, s.NormId })
            .ToListAsync(cancellationToken);

        if (sourceRows.Count == 0)
        {
            await PopulateSnapshotAsync(context, targetVersionId, targetCreatedAt, cancellationToken);
            return;
        }

        foreach (var row in CreateCopies(sourceRows.Select(s => (s.WorkDirectionId, s.NormId)), targetVersionId))
            context.AnalysisVersionSnapshots.Add(row);
    }

    public static IReadOnlyList<AnalysisVersionSnapshot> CreateCopies(
        IEnumerable<(int WorkDirectionId, int NormId)> source,
        int targetVersionId) =>
        source
            .Select(s => new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = targetVersionId,
                WorkDirectionId = s.WorkDirectionId,
                NormId = s.NormId,
            })
            .ToList();

    /// <summary>Стабильный ключ состава снимка: «wdId:normId» по возрастанию.</summary>
    public static string? FormatKey(IEnumerable<(int WorkDirectionId, int NormId)> rows)
    {
        var parts = rows
            .OrderBy(r => r.WorkDirectionId)
            .ThenBy(r => r.NormId)
            .Select(r => $"{r.WorkDirectionId}:{r.NormId}")
            .ToList();
        return parts.Count == 0 ? null : string.Join(",", parts);
    }
}
