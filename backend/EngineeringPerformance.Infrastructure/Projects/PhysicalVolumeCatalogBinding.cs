using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

/// <summary>Привязка ProjectPhysicalVolume к каталогу КФО или БЭР по виду версии.</summary>
public static class PhysicalVolumeCatalogBinding
{
    public const string KfoTypeName = "КФО";
    public const string BerTypeName = "БЭР";

    public static async Task<bool> IsKfoVersionAsync(AppDbContext context, int versionId, CancellationToken cancellationToken = default)
    {
        var typeName = await context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.Id == versionId)
            .Select(v => v.PhysicalVolumeType!.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return string.Equals(typeName, KfoTypeName, StringComparison.Ordinal);
    }

    public static async Task<bool> CatalogEntryExistsAsync(
        AppDbContext context,
        int versionId,
        int catalogId,
        CancellationToken cancellationToken = default)
    {
        return await IsKfoVersionAsync(context, versionId, cancellationToken)
            ? await context.PhysicalVolumesKfo.AnyAsync(x => x.Id == catalogId, cancellationToken)
            : await context.PhysicalVolumesBer.AnyAsync(x => x.Id == catalogId, cancellationToken);
    }

    public static void AssignCatalogId(ProjectPhysicalVolume entity, int catalogId, bool isKfo)
    {
        if (isKfo)
        {
            entity.PhysicalVolumeKfoId = catalogId;
            entity.PhysicalVolumeBerId = null;
        }
        else
        {
            entity.PhysicalVolumeBerId = catalogId;
            entity.PhysicalVolumeKfoId = null;
        }
    }

    public static void AssignLaborCatalogId(ProjectLaborIntensity entity, int? catalogId, bool isKfo)
    {
        if (!catalogId.HasValue)
        {
            entity.PhysicalVolumeKfoId = null;
            entity.PhysicalVolumeBerId = null;
            return;
        }

        if (isKfo)
        {
            entity.PhysicalVolumeKfoId = catalogId;
            entity.PhysicalVolumeBerId = null;
        }
        else
        {
            entity.PhysicalVolumeBerId = catalogId;
            entity.PhysicalVolumeKfoId = null;
        }
    }
}
