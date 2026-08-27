using System.Security.Cryptography;
using System.Text;

namespace EngineeringPerformance.Infrastructure.Directories;

internal static class EquipmentCatalogKeyHasher
{
    /// <summary>
    /// ProjectId = null → legacy hash (Name + UnitId) для номенклатуры.
    /// ProjectId задан → ProjectId + Name + UnitId.
    /// </summary>
    public static string Compute(int? projectId, string name, int? unitId)
    {
        var trimmedName = name.Trim();
        var unitPart = unitId?.ToString() ?? string.Empty;
        var payload = projectId is int pid
            ? $"{pid}\0{trimmedName}\0{unitPart}"
            : $"{trimmedName}\0{unitPart}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Obsolete("Use Compute(projectId, name, unitId)")]
    public static string Compute(string name, int? unitId) => Compute(projectId: null, name, unitId);
}
