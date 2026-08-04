using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectVersionPlanningParameterService(
    AppDbContext context,
    IAnalysisVersionRecalculationOrchestrator recalcOrchestrator) : IProjectVersionPlanningParameterService
{
    private readonly AppDbContext _context = context;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator = recalcOrchestrator;

    public async Task<IReadOnlyList<ProjectVersionPlanningParameterDto>> ListAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionExistsAsync(projectId, versionId, cancellationToken);
        await SeedForVersionAsync(versionId, cancellationToken);

        var rows = await _context.ProjectVersionPlanningParameters.AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .Join(
                _context.IicWorkDirections.AsNoTracking(),
                p => p.IicWorkDirectionId,
                d => d.Id,
                (p, d) => new { p, d })
            .GroupJoin(
                _context.UnitsOfMeasurement.AsNoTracking(),
                x => x.d.UnitId,
                u => u.Id,
                (x, units) => new { x.p, x.d, unit = units.FirstOrDefault() })
            .OrderBy(x => x.d.SortOrder)
            .ThenBy(x => x.d.Name)
            .Select(x => new ProjectVersionPlanningParameterDto(
                x.p.Id,
                x.p.IicWorkDirectionId,
                x.d.ParentId,
                x.d.SortOrder,
                x.d.Code,
                x.d.Name,
                x.unit != null ? x.unit.Name : null,
                x.p.ZPessimistic,
                x.p.ZBase,
                x.p.ZOptimistic,
                x.p.ZActual,
                0m))
            .ToListAsync(cancellationToken);

        var weights = await LoadLaborHoursWeightsAsync(versionId, cancellationToken);
        if (weights.Count == 0)
            return rows;

        return rows
            .Select(r => r with
            {
                LaborHoursWeight = weights.GetValueOrDefault(r.IicWorkDirectionId),
            })
            .ToList();
    }

    public async Task SaveAsync(
        int projectId,
        int versionId,
        SaveProjectVersionPlanningParametersRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionExistsAsync(projectId, versionId, cancellationToken);
        await SeedForVersionAsync(versionId, cancellationToken);

        var existing = await _context.ProjectVersionPlanningParameters
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .ToDictionaryAsync(p => p.IicWorkDirectionId, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!existing.TryGetValue(item.IicWorkDirectionId, out var row))
                continue;

            row.ZPessimistic = item.ZPessimistic;
            row.ZBase = item.ZBase;
            row.ZOptimistic = item.ZOptimistic;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);
    }

    public async Task SeedForVersionAsync(int versionId, CancellationToken cancellationToken)
    {
        var hasAny = await _context.ProjectVersionPlanningParameters
            .AnyAsync(p => p.ProjectAnalysisVersionId == versionId, cancellationToken);
        if (hasAny)
            return;

        var directions = await _context.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        foreach (var directionId in directions)
        {
            _context.ProjectVersionPlanningParameters.Add(new ProjectVersionPlanningParameter
            {
                ProjectAnalysisVersionId = versionId,
                IicWorkDirectionId = directionId,
                ZPessimistic = 0.5m,
                ZBase = 0m,
                ZOptimistic = -0.5m,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CopyFromVersionAsync(int sourceVersionId, int targetVersionId, CancellationToken cancellationToken = default)
    {
        await SeedForVersionAsync(sourceVersionId, cancellationToken);
        await SeedForVersionAsync(targetVersionId, cancellationToken);

        var sourceRows = await _context.ProjectVersionPlanningParameters.AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        var targetRows = await _context.ProjectVersionPlanningParameters
            .Where(p => p.ProjectAnalysisVersionId == targetVersionId)
            .ToDictionaryAsync(p => p.IicWorkDirectionId, cancellationToken);

        foreach (var source in sourceRows)
        {
            if (!targetRows.TryGetValue(source.IicWorkDirectionId, out var target))
                continue;

            target.ZPessimistic = source.ZPessimistic;
            target.ZBase = source.ZBase;
            target.ZOptimistic = source.ZOptimistic;
            target.ZActual = source.ZActual;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Веса = сумма уже посчитанной LaborIntensityBase по классификатору направления (КФО/БЭР).
    /// </summary>
    private async Task<Dictionary<int, decimal>> LoadLaborHoursWeightsAsync(
        int versionId,
        CancellationToken cancellationToken)
    {
        var kfoWeights = await _context.ProjectPhysicalVolumes.AsNoTracking()
            .Where(pv => pv.ProjectAnalysisVersionId == versionId && pv.PhysicalVolumeKfoId != null)
            .Join(
                _context.PhysicalVolumesKfo.AsNoTracking(),
                pv => pv.PhysicalVolumeKfoId,
                k => k.Id,
                (pv, k) => new { k.ClassifierId, Hours = pv.LaborIntensityBase ?? 0m })
            .Where(x => x.ClassifierId != null)
            .GroupBy(x => x.ClassifierId!.Value)
            .Select(g => new { DirectionId = g.Key, Hours = g.Sum(x => x.Hours) })
            .ToListAsync(cancellationToken);

        var berWeights = await _context.ProjectPhysicalVolumes.AsNoTracking()
            .Where(pv => pv.ProjectAnalysisVersionId == versionId && pv.PhysicalVolumeBerId != null)
            .Join(
                _context.PhysicalVolumesBer.AsNoTracking(),
                pv => pv.PhysicalVolumeBerId,
                b => b.Id,
                (pv, b) => new { b.ClassifierId, Hours = pv.LaborIntensityBase ?? 0m })
            .Where(x => x.ClassifierId != null)
            .GroupBy(x => x.ClassifierId!.Value)
            .Select(g => new { DirectionId = g.Key, Hours = g.Sum(x => x.Hours) })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, decimal>();
        foreach (var row in kfoWeights.Concat(berWeights))
        {
            map[row.DirectionId] = map.GetValueOrDefault(row.DirectionId) + row.Hours;
        }

        return map;
    }

    private async Task EnsureVersionExistsAsync(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var exists = await _context.ProjectAnalysisVersions.AsNoTracking()
            .AnyAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Version {versionId} not found for project {projectId}.");
    }
}
