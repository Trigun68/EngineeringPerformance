using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.LaborIntensityServices;

public interface ILaborIntensityRecalculationService
{
    Task<int> RecalculateAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
}

public sealed class LaborIntensityRecalculationService : ILaborIntensityRecalculationService
{
    private readonly AppDbContext _context;
    private readonly IDeviationCalculator _deviationCalculator;

    public LaborIntensityRecalculationService(AppDbContext context, IDeviationCalculator deviationCalculator)
    {
        _context = context;
        _deviationCalculator = deviationCalculator;
    }

    public async Task<int> RecalculateAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var version = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Version not found.");

        var ppvs = await _context.ProjectPhysicalVolumes
            .Include(x => x.PhysicalVolumeKfo)
            .Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var snapshots = await _context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .Include(s => s.Norm)
            .ToListAsync(cancellationToken);

        var norms = snapshots
            .Where(s => s.Norm is not null)
            .GroupBy(s => s.WorkDirectionId)
            .ToDictionary(g => g.Key, g => NormSnapshotFactory.FromEntity(g.First().Norm!));

        var zRows = await _context.ProjectVersionPlanningParameters
            .AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var zParams = zRows.ToDictionary(
            p => p.IicWorkDirectionId,
            p => new Application.Analytics.PlanningZParameters(p.ZPessimistic, p.ZBase, p.ZOptimistic));

        var lines = ppvs.Select(pv => new LaborIntensityCalculator.PpvLine(
            pv.Id,
            pv.GetClassifierId(),
            pv.FacilityCode,
            pv.Quantity,
            pv.GetCoefficient(),
            pv.GetIsAssessment() == true)).ToList();

        var results = LaborIntensityCalculator.Recalculate(
            version.PlanningScenario,
            lines,
            norms,
            zParams,
            _deviationCalculator);

        var byId = results.ToDictionary(r => r.PpvId);
        foreach (var pv in ppvs)
        {
            if (!byId.TryGetValue(pv.Id, out var r)) continue;
            pv.LaborIntensityBase = r.Base;
            pv.LaborIntensityPessimistic = r.Pessimistic;
            pv.LaborIntensityOptimistic = r.Optimistic;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}
