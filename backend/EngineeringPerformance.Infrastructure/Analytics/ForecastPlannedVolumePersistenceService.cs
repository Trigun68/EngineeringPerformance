using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class ForecastPlannedVolumePersistenceService : IForecastPlannedVolumePersistenceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;

    public ForecastPlannedVolumePersistenceService(
        IDbContextFactory<AppDbContext> contextFactory,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator)
    {
        _contextFactory = contextFactory;
        _recalcOrchestrator = recalcOrchestrator;
    }

    public async Task SavePlannedVolumeAsync(
        int projectId,
        int projectAnalysisVersionId,
        int workDirectionId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Плановый объём не может быть отрицательным.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == projectAnalysisVersionId && v.ProjectId == projectId, cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException("Версия анализа не найдена или не принадлежит проекту.");
        }

        if (version.Direction != AnalysisDirection.Plan)
        {
            throw new InvalidOperationException("Редактирование плановых объёмов доступно только для версии с направлением «План».");
        }

        var rows = await context.ProjectPhysicalVolumes
            .Include(pv => pv.PhysicalVolumeKfo)
            .Include(pv => pv.PhysicalVolumeBer)
            .Where(pv => pv.ProjectAnalysisVersionId == projectAnalysisVersionId
                         && (pv.PhysicalVolumeKfo != null && pv.PhysicalVolumeKfo.IsAssessment == true
                             && pv.PhysicalVolumeKfo.ClassifierId == workDirectionId
                             || pv.PhysicalVolumeBer != null && pv.PhysicalVolumeBer.IsAssessment == true
                             && pv.PhysicalVolumeBer.ClassifierId == workDirectionId))
            .OrderBy(pv => pv.Id)
            .ToListAsync(cancellationToken);

        if (rows.Count > 0)
        {
            var newQuantities = PlannedVolumeDistributor.Distribute(
                version.PlanningScenario,
                quantity,
                rows.Select(r => (r.GetCoefficient(), r.Quantity)).ToList());

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Quantity = newQuantities[i];
                rows[i].ModifiedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            await _recalcOrchestrator.RecalculateVersionAsync(projectId, projectAnalysisVersionId, cancellationToken);
            return;
        }

        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(context, projectAnalysisVersionId, cancellationToken);

        if (isKfo)
        {
            var catalogKfo = await context.PhysicalVolumesKfo
                .AsNoTracking()
                .Where(pv => !pv.IsArchived && pv.IsAssessment == true && pv.ClassifierId == workDirectionId)
                .OrderBy(pv => pv.SortOrder)
                .ThenBy(pv => pv.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogKfo is null)
            {
                throw new InvalidOperationException(
                    "Не найдена номенклатура КФО с классификатором IIC для этой группы.");
            }

            var entity = new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = projectAnalysisVersionId,
                Quantity = ProjectPhysicalVolumeIicUnits.ProjectQuantityFromIicGroupUnits(quantity, catalogKfo.Coefficient),
                CreatedAt = DateTime.UtcNow,
            };
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, catalogKfo.Id, isKfo: true);
            PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
                entity, catalogKfo.Coefficient, catalogKfo.IsAssessment, catalogKfo.ClassifierId);
            context.ProjectPhysicalVolumes.Add(entity);
        }
        else
        {
            var catalogBer = await context.PhysicalVolumesBer
                .AsNoTracking()
                .Where(pv => !pv.IsArchived && pv.IsAssessment == true && pv.ClassifierId == workDirectionId)
                .OrderBy(pv => pv.SortOrder)
                .ThenBy(pv => pv.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogBer is null)
            {
                throw new InvalidOperationException(
                    "Не найдена номенклатура БЕР с классификатором IIC для этой группы.");
            }

            var entity = new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = projectAnalysisVersionId,
                Quantity = ProjectPhysicalVolumeIicUnits.ProjectQuantityFromIicGroupUnits(quantity, catalogBer.Coefficient),
                CreatedAt = DateTime.UtcNow,
            };
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, catalogBer.Id, isKfo: false);
            PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
                entity, catalogBer.Coefficient, catalogBer.IsAssessment, catalogBer.ClassifierId);
            context.ProjectPhysicalVolumes.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        await _recalcOrchestrator.RecalculateVersionAsync(projectId, projectAnalysisVersionId, cancellationToken);
    }
}
