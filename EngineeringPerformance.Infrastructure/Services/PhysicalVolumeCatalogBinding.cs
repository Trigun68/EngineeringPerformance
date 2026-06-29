using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

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
}
