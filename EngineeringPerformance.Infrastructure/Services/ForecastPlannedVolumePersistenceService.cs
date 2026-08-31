using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ForecastPlannedVolumePersistenceService : IForecastPlannedVolumePersistenceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ForecastPlannedVolumePersistenceService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SavePlannedVolumeAsync(
        int projectId,
        int projectAnalysisVersionId,
        int workDirectionId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Плановый объём не может быть отрицательным.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == projectAnalysisVersionId && v.ProjectId == projectId, cancellationToken);

        if (version is null)
            throw new InvalidOperationException("Версия анализа не найдена или не принадлежит проекту.");

        if (version.Direction != AnalysisDirection.Plan)
            throw new InvalidOperationException("Редактирование плановых объёмов доступно только для версии с направлением «План».");

        var rows = await context.ProjectPhysicalVolumes
            .Include(pv => pv.PhysicalVolumeKfo)
            .Include(pv => pv.PhysicalVolumeBer)
            .Where(pv => pv.ProjectAnalysisVersionId == projectAnalysisVersionId
                         && (pv.PhysicalVolumeKfo != null && pv.PhysicalVolumeKfo.ClassifierId == workDirectionId
                             || pv.PhysicalVolumeBer != null && pv.PhysicalVolumeBer.ClassifierId == workDirectionId))
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
            return;
        }

        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(context, projectAnalysisVersionId, cancellationToken);

        if (isKfo)
        {
            var catalogKfo = await context.PhysicalVolumesKfo
                .AsNoTracking()
                .Where(pv => !pv.IsArchived && pv.ClassifierId == workDirectionId)
                .OrderBy(pv => pv.SortOrder)
                .ThenBy(pv => pv.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogKfo is null)
                throw new InvalidOperationException(
                    "Не найдена номенклатура КФО с классификатором ИИС для этой группы.");

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
                .Where(pv => !pv.IsArchived && pv.ClassifierId == workDirectionId)
                .OrderBy(pv => pv.SortOrder)
                .ThenBy(pv => pv.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogBer is null)
                throw new InvalidOperationException(
                    "Не найдена номенклатура БЕР с классификатором ИИС для этой группы.");

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
    }
}
