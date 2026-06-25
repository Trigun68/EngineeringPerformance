using System.Security.Cryptography;
using System.Text;

namespace EngineeringPerformance.Infrastructure.Directories;

internal static class EquipmentCatalogKeyHasher
{
    public static string Compute(string name, int? unitId)
    {
        var payload = $"{name.Trim()}\0{unitId?.ToString() ?? string.Empty}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
