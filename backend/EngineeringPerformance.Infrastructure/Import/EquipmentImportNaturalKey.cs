using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Натуральный ключ строки оборудования проекта для Replace/Upsert в рамках версии.
/// </summary>
internal readonly record struct EquipmentImportNaturalKey(
    int EquipmentId,
    string FacilityCode,
    string DocumentSet,
    string FullEquipmentId,
    string Tag)
{
    public static EquipmentImportNaturalKey FromEntity(ProjectEquipment entity) =>
        new(
            entity.EquipmentId,
            entity.FacilityCode ?? string.Empty,
            entity.DocumentSet ?? string.Empty,
            entity.FullEquipmentId ?? string.Empty,
            entity.Tag ?? string.Empty);
}
