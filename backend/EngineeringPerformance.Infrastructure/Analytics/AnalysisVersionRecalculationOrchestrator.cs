using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class AnalysisVersionRecalculationOrchestrator(
    AppDbContext context,
    ILaborIntensityRecalculationService recalculation,
    IProjectAnalyticsCache analyticsCache,
    ILogger<AnalysisVersionRecalculationOrchestrator> logger,
    IUserActionLogger actionLogger) : IAnalysisVersionRecalculationOrchestrator
{
    private readonly AppDbContext _context = context;
    private readonly ILaborIntensityRecalculationService _recalculation = recalculation;
    private readonly IProjectAnalyticsCache _analyticsCache = analyticsCache;
    private readonly ILogger<AnalysisVersionRecalculationOrchestrator> _logger = logger;
    private readonly IUserActionLogger _actionLogger = actionLogger;

    public Task<int> RecalculateVersionAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default) =>
        RecalculateOneAsync(projectId, versionId, "recalculate", cancellationToken);

    public Task<int> RecalculateVersionAsync(
        int projectId,
        int versionId,
        string trigger,
        CancellationToken cancellationToken = default) =>
        RecalculateOneAsync(
            projectId,
            versionId,
            string.IsNullOrWhiteSpace(trigger) ? "recalculate" : trigger,
            cancellationToken);

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
            await RecalculateOneAsync(version.ProjectId, version.Id, "norm", cancellationToken);
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

        await RefreshPpvCatalogSnapshotsAsync(key, catalogId, activeSet, cancellationToken);

        var count = 0;
        foreach (var (projectId, versionId) in affected.Where(a => activeSet.Contains(a.VersionId)).Distinct())
        {
            await RecalculateOneAsync(projectId, versionId, "catalog", cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<int> RecalculateAllVersionsAsync(
        bool includeArchived = true,
        CancellationToken cancellationToken = default)
    {
        var versions = await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => includeArchived || !v.IsArchived)
            .Select(v => new { v.Id, v.ProjectId })
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var version in versions)
        {
            await RecalculateOneAsync(version.ProjectId, version.Id, "recalculate-all", cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<IReadOnlyList<VersionCalculationLogDto>> ListLogsAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var belongs = await _context.ProjectAnalysisVersions.AsNoTracking()
            .AnyAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);
        if (!belongs)
            return [];

        return await _context.VersionCalculationLogs.AsNoTracking()
            .Where(l => l.ProjectAnalysisVersionId == versionId)
            .OrderByDescending(l => l.Utc)
            .Take(50)
            .Select(l => new VersionCalculationLogDto(
                l.Id,
                l.Utc,
                l.Trigger,
                l.PlanningScenario,
                l.SnapshotRowCount,
                l.PpvRowCount,
                l.EquipmentRowCount,
                l.BaseHoursSum,
                l.OptimisticHoursSum,
                l.PessimisticHoursSum))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> RecalculateOneAsync(
        int projectId,
        int versionId,
        string trigger,
        CancellationToken cancellationToken)
    {
        var updated = await _recalculation.RecalculateAsync(projectId, versionId, cancellationToken);
        await WriteCalculationLogAsync(projectId, versionId, trigger, cancellationToken);
        // Persist колонок уже SoT; кэш сбрасываем. Первый GET page-data сам заполнит Redis
        // (singleflight), без warm на том же запросе что persist.
        await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);
        _logger.LogDebug(
            "Recalculated labor intensity for project {ProjectId} version {VersionId}: {Updated} rows.",
            projectId,
            versionId,
            updated);

        // Журнал аудита — только кнопка «Пересчитать», не каскад import/scenario/catalog.
        if (string.Equals(trigger, "button", StringComparison.Ordinal))
        {
            await _actionLogger.LogAsync(
                UserActionType.VersionRecalculate,
                entityType: "ProjectAnalysisVersion",
                entityId: versionId,
                description: $"Recalculated version {versionId}",
                cancellationToken: cancellationToken);
        }

        return updated;
    }

    private async Task WriteCalculationLogAsync(
        int projectId,
        int versionId,
        string trigger,
        CancellationToken cancellationToken)
    {
        var version = await _context.ProjectAnalysisVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);
        if (version is null)
            return;

        var snapshotRowCount = await _context.AnalysisVersionSnapshots.AsNoTracking()
            .CountAsync(s => s.ProjectAnalysisVersionId == versionId, cancellationToken);

        var ppvAgg = await _context.ProjectPhysicalVolumes.AsNoTracking()
            .Where(pv => pv.ProjectAnalysisVersionId == versionId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Base = g.Sum(x => x.LaborIntensityBase ?? 0m),
                Optimistic = g.Sum(x => x.LaborIntensityOptimistic ?? 0m),
                Pessimistic = g.Sum(x => x.LaborIntensityPessimistic ?? 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var eqAgg = await _context.ProjectEquipments.AsNoTracking()
            .Where(eq => eq.ProjectAnalysisVersionId == versionId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Base = g.Sum(x => x.LaborIntensityBase ?? 0m),
                Optimistic = g.Sum(x => x.LaborIntensityOptimistic ?? 0m),
                Pessimistic = g.Sum(x => x.LaborIntensityPessimistic ?? 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        _context.VersionCalculationLogs.Add(new VersionCalculationLog
        {
            ProjectAnalysisVersionId = versionId,
            Utc = DateTime.UtcNow,
            Trigger = string.IsNullOrWhiteSpace(trigger) ? "recalculate" : trigger,
            PlanningScenario = version.PlanningScenario,
            SnapshotRowCount = snapshotRowCount,
            PpvRowCount = ppvAgg?.Count ?? 0,
            EquipmentRowCount = eqAgg?.Count ?? 0,
            BaseHoursSum = (ppvAgg?.Base ?? 0m) + (eqAgg?.Base ?? 0m),
            OptimisticHoursSum = (ppvAgg?.Optimistic ?? 0m) + (eqAgg?.Optimistic ?? 0m),
            PessimisticHoursSum = (ppvAgg?.Pessimistic ?? 0m) + (eqAgg?.Pessimistic ?? 0m),
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshPpvCatalogSnapshotsAsync(
        string resourceKey,
        int catalogId,
        HashSet<int> activeVersionIds,
        CancellationToken cancellationToken)
    {
        if (activeVersionIds.Count == 0)
            return;

        if (resourceKey == "physical-volumes-kfo")
        {
            var catalog = await _context.PhysicalVolumesKfo.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == catalogId, cancellationToken);
            if (catalog is null)
                return;

            var rows = await _context.ProjectPhysicalVolumes
                .Where(pv => pv.PhysicalVolumeKfoId == catalogId
                             && activeVersionIds.Contains(pv.ProjectAnalysisVersionId))
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
                PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
                    row, catalog.Coefficient, catalog.IsAssessment, catalog.ClassifierId);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (resourceKey != "physical-volumes-ber")
            return;

        var ber = await _context.PhysicalVolumesBer.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == catalogId, cancellationToken);
        if (ber is null)
            return;

        var berRows = await _context.ProjectPhysicalVolumes
            .Where(pv => pv.PhysicalVolumeBerId == catalogId
                         && activeVersionIds.Contains(pv.ProjectAnalysisVersionId))
            .ToListAsync(cancellationToken);
        foreach (var row in berRows)
            PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
                row, ber.Coefficient, ber.IsAssessment, ber.ClassifierId);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
