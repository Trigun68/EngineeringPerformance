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
    private readonly IProjectAnalyticsCache _analyticsCache;

    public ForecastPlannedVolumePersistenceService(
        IDbContextFactory<AppDbContext> contextFactory,
        IProjectAnalyticsCache analyticsCache)
    {
        _contextFactory = contextFactory;
        _analyticsCache = analyticsCache;
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
            var first = rows[0];
            var k = first.GetCoefficient();
            first.Quantity = ProjectPhysicalVolumeIicUnits.ProjectQuantityFromIicGroupUnits(quantity, k);
            first.ModifiedAt = DateTime.UtcNow;

            for (var i = 1; i < rows.Count; i++)
            {
                rows[i].Quantity = 0m;
                rows[i].ModifiedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);
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
                    "Не найдена номенклатура КФО с классификатором ИИС для этой группы.");
            }

            var entity = new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = projectAnalysisVersionId,
                Quantity = ProjectPhysicalVolumeIicUnits.ProjectQuantityFromIicGroupUnits(quantity, catalogKfo.Coefficient),
                CreatedAt = DateTime.UtcNow,
            };
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, catalogKfo.Id, isKfo: true);
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
                    "Не найдена номенклатура БЭР с классификатором ИИС для этой группы.");
            }

            var entity = new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = projectAnalysisVersionId,
                Quantity = ProjectPhysicalVolumeIicUnits.ProjectQuantityFromIicGroupUnits(quantity, catalogBer.Coefficient),
                CreatedAt = DateTime.UtcNow,
            };
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, catalogBer.Id, isKfo: false);
            context.ProjectPhysicalVolumes.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);
    }
}
