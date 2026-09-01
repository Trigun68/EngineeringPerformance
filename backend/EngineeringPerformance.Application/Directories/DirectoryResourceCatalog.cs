using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Directories;

public sealed record DirectoryResourceMeta(
    string Slug,
    PermissionType Permission,
    bool IsHierarchical);

public static class DirectoryResourceCatalog
{
    public static readonly IReadOnlyList<DirectoryResourceMeta> All =
    [
        new("physical-volumes-kfo", PermissionType.PhysicalVolumes, true),
        new("physical-volumes-ber", PermissionType.PhysicalVolumes, true),
        new("equipment", PermissionType.Equipment, true),
        new("labor-intensities", PermissionType.LaborIntensities, true),
        new("iic-work-directions", PermissionType.IicWorkDirections, true),
        new("units-of-measurement", PermissionType.UnitsOfMeasurement, false),
        new("doc-sets", PermissionType.DocSets, false),
        new("project-characteristics", PermissionType.ProjectCharacteristics, false),
    ];

    private static readonly Dictionary<string, DirectoryResourceMeta> BySlug =
        All.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string slug, out DirectoryResourceMeta meta) =>
        BySlug.TryGetValue(slug, out meta!);
}
