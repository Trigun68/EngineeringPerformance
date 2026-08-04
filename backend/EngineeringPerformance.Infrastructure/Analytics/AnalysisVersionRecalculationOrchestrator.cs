using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class AnalysisVersionRecalculationOrchestrator(
    AppDbContext context,
    ILaborIntensityRecalculationService recalculation) : IAnalysisVersionRecalculationOrchestrator
{
    private readonly AppDbContext _context = context;
    private readonly ILaborIntensityRecalculationService _recalculation = recalculation;

    public Task<int> RecalculateVersionAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        RecalculateOneAsync(projectId, versionId, cancellationToken);

    public async Task<int> AfterNormChangedAsync(
        int workDirectionId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.SpecifyKind(effectiveFrom.Date, DateTimeKind.Utc);
        var to = effectiveTo.HasValue
            ? DateTime.SpecifyKind(effectiveTo.Value.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.MaxValue.Date, DateTimeKind.Utc);

        // Дата версии (CreatedAt) — as-of для выбора нормы.
        var versions = await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => !v.IsArchived)
            .Select(v => new { v.Id, v.ProjectId, v.CreatedAt })
            .ToListAsync(cancellationToken);

        var dateAffected = versions
            .Where(v =>
            {
                var day = v.CreatedAt.ToUniversalTime().Date;
                return day >= from.Date && day <= to.Date;
            })
            .ToList();

        if (dateAffected.Count == 0)
            return 0;

        var candidateIds = dateAffected.Select(v => v.Id).ToList();

        // Сужение по направлению: версии со снимком другого направления не трогаем;
        // версии без снимка оставляем (Repopulate создаст строки).
        var snapshotRows = await _context.AnalysisVersionSnapshots.AsNoTracking()
            .Where(s => candidateIds.Contains(s.ProjectAnalysisVersionId))
            .Select(s => new { s.ProjectAnalysisVersionId, s.WorkDirectionId })
            .ToListAsync(cancellationToken);

        var versionsWithAnySnapshot = snapshotRows
            .Select(s => s.ProjectAnalysisVersionId)
            .ToHashSet();
        var versionsWithDirection = snapshotRows
            .Where(s => s.WorkDirectionId == workDirectionId)
            .Select(s => s.ProjectAnalysisVersionId)
            .ToHashSet();

        var affected = dateAffected
            .Where(v => versionsWithDirection.Contains(v.Id) || !versionsWithAnySnapshot.Contains(v.Id))
            .ToList();

        if (affected.Count == 0)
            return 0;

        var count = 0;
        foreach (var version in affected)
        {
            await AnalysisVersionSnapshotService.RepopulateSnapshotAsync(
                _context, version.Id, version.CreatedAt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await RecalculateOneAsync(version.ProjectId, version.Id, cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<int> AfterCatalogAssessmentOrCoefficientChangedAsync(
        string resource,
        int catalogId,
        CancellationToken cancellationToken = default)
    {
        var key = resource.ToLowerInvariant();
        List<(int ProjectId, int VersionId)> affected;
        switch (key)
        {
            case "physical-volumes-kfo":
            {
                var rows = await _context.ProjectPhysicalVolumes.AsNoTracking()
                    .Where(pv => pv.PhysicalVolumeKfoId == catalogId)
                    .Select(pv => new { pv.ProjectId, VersionId = pv.ProjectAnalysisVersionId })
                    .Distinct()
                    .ToListAsync(cancellationToken);
                affected = rows.Select(r => (r.ProjectId, r.VersionId)).ToList();
                break;
            }
            case "physical-volumes-ber":
            {
                var rows = await _context.ProjectPhysicalVolumes.AsNoTracking()
                    .Where(pv => pv.PhysicalVolumeBerId == catalogId)
                    .Select(pv => new { pv.ProjectId, VersionId = pv.ProjectAnalysisVersionId })
                    .Distinct()
                    .ToListAsync(cancellationToken);
                affected = rows.Select(r => (r.ProjectId, r.VersionId)).ToList();
                break;
            }
            case "equipment":
            {
                var rows = await _context.ProjectEquipments.AsNoTracking()
                    .Where(pe => pe.EquipmentId == catalogId)
                    .Select(pe => new { pe.ProjectId, VersionId = pe.ProjectAnalysisVersionId })
                    .Distinct()
                    .ToListAsync(cancellationToken);
                affected = rows.Select(r => (r.ProjectId, r.VersionId)).ToList();
                break;
            }
            default:
                affected = [];
                break;
        }

        if (affected.Count == 0)
            return 0;

        // Только неархивные версии.
        var versionIds = affected.Select(a => a.VersionId).Distinct().ToList();
        var activeIds = await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => versionIds.Contains(v.Id) && !v.IsArchived)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);
        var activeSet = activeIds.ToHashSet();

        var count = 0;
        foreach (var (projectId, versionId) in affected.Where(a => activeSet.Contains(a.VersionId)).Distinct())
        {
            await RecalculateOneAsync(projectId, versionId, cancellationToken);
            count++;
        }

        return count;
    }

    private async Task<int> RecalculateOneAsync(int projectId, int versionId, CancellationToken cancellationToken)
    {
        await _recalculation.RecalculateAsync(projectId, versionId, cancellationToken);
        return 1;
    }
}
