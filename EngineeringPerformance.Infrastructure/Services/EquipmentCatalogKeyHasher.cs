using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Хеш ключа номенклатуры (Name + UnitId) для уникального индекса без лимита 1700 байт SQL Server.
/// </summary>
public static class EquipmentCatalogKeyHasher
{
    public static string Compute(string name, int? unitId)
    {
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{name}\0{unitId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
