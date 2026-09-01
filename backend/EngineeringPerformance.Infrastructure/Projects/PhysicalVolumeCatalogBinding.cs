using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

/// <summary>Привязка ProjectPhysicalVolume к каталогу КФО или БЕР по виду версии.</summary>
public static class PhysicalVolumeCatalogBinding
{
    public const string KfoTypeName = "КФО";
    public const string BerTypeName = "БЕР";

    /// <summary>Каноническое «БЕР» и прежнее написание в уже существующей БД до миграции 114.</summary>
    public static bool IsBerTypeName(string? name) =>
        string.Equals(name, BerTypeName, StringComparison.Ordinal)
        || string.Equals(name, "БЭР", StringComparison.Ordinal);

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

    /// <summary>Проверяет, что для версии выбран справочник КФО или БЕР.</summary>
    public static async Task EnsureCatalogTypeConfiguredAsync(
        AppDbContext context,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var typeName = await GetCatalogTypeNameAsync(context, versionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(typeName)
            || (!string.Equals(typeName, KfoTypeName, StringComparison.Ordinal)
                && !IsBerTypeName(typeName)))
        {
            throw new InvalidOperationException(
                "Для версии проекта не выбран справочник физобъёмов. Укажите КФО или БЕР в параметрах версии.");
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

    public static void FillCatalogSnapshot(
        ProjectPhysicalVolume entity,
        decimal coefficient,
        bool? isAssessment,
        int? classifierId)
    {
        entity.CatalogCoefficient = coefficient <= 0m ? 1m : coefficient;
        entity.CatalogIsAssessment = isAssessment;
        entity.CatalogClassifierId = classifierId;
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
