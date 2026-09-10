using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.LaborIntensityServices;

public interface ILaborIntensityRecalculationService
{
    Task<int> RecalculateAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
}

public sealed class LaborIntensityRecalculationService : ILaborIntensityRecalculationService
{
    private readonly IAnalyticsDbContextFactory _analyticsDb;
    private readonly IPerformanceCalculationEngine _calculationEngine;

    public LaborIntensityRecalculationService(
        IAnalyticsDbContextFactory analyticsDb,
        IPerformanceCalculationEngine calculationEngine)
    {
        _analyticsDb = analyticsDb;
        _calculationEngine = calculationEngine;
    }

    public async Task<int> RecalculateAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        await using var context = await _analyticsDb.CreateDbContextAsync(cancellationToken);
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Version not found.");

        var ppvs = await context.ProjectPhysicalVolumes
            .Include(x => x.PhysicalVolumeKfo)
            .Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var equipment = await context.ProjectEquipments
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var snapshots = await context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.ProjectAnalysisVersionId == versionId)
            .Include(s => s.Norm)
            .ToListAsync(cancellationToken);

        var norms = snapshots
            .Where(s => s.Norm is not null)
            .GroupBy(s => s.WorkDirectionId)
            .ToDictionary(g => g.Key, g => NormSnapshotFactory.FromEntity(g.First().Norm!));

        var zRows = await context.ProjectVersionPlanningParameters
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

        var equipmentLines = equipment.Select(eq => new LaborIntensityCalculator.EquipmentLine(
            eq.Id,
            eq.Equipment?.ClassifierId,
            eq.QuantityInIicGroupUnits(),
            eq.Equipment?.IsAssessment == true,
            eq.FacilityCode)).ToList();

        var planHours = _calculationEngine.RecalculatePlanHours(
            version.PlanningScenario,
            lines,
            equipmentLines,
            norms,
            zParams);

        var byId = planHours.Ppv.ToDictionary(r => r.PpvId);
        foreach (var pv in ppvs)
        {
            if (byId.TryGetValue(pv.Id, out var r))
            {
                pv.LaborIntensityBase = r.Base;
                pv.LaborIntensityPessimistic = r.Pessimistic;
                pv.LaborIntensityOptimistic = r.Optimistic;
            }
            else
            {
                pv.LaborIntensityBase = null;
                pv.LaborIntensityPessimistic = null;
                pv.LaborIntensityOptimistic = null;
            }
        }

        foreach (var eq in equipment)
        {
            if (planHours.Equipment.TryGetValue(eq.Id, out var hours))
            {
                eq.LaborIntensityBase = hours.Base;
                eq.LaborIntensityPessimistic = hours.Pessimistic;
                eq.LaborIntensityOptimistic = hours.Optimistic;
            }
            else
            {
                eq.LaborIntensityBase = null;
                eq.LaborIntensityPessimistic = null;
                eq.LaborIntensityOptimistic = null;
            }
        }

        return await context.SaveChangesAsync(cancellationToken);
    }
}
