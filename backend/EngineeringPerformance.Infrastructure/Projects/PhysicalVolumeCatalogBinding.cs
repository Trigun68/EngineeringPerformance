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
        var typeName = await GetCatalogTypeNameAsync(context, versionId, cancellationToken);
        return string.Equals(typeName, KfoTypeName, StringComparison.Ordinal);
    }

    public static async Task<string?> GetCatalogTypeNameAsync(
        AppDbContext context,
        int versionId,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.Id == versionId)
            .Select(v => v.PhysicalVolumeType != null ? v.PhysicalVolumeType.Name : null)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Проверяет, что для версии выбран справочник КФО или БЭР.</summary>
    public static async Task EnsureCatalogTypeConfiguredAsync(
        AppDbContext context,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var typeName = await GetCatalogTypeNameAsync(context, versionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(typeName)
            || (!string.Equals(typeName, KfoTypeName, StringComparison.Ordinal)
                && !string.Equals(typeName, BerTypeName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Для версии проекта не выбран справочник физобъёмов. Укажите КФО или БЭР в параметрах версии.");
        }
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
