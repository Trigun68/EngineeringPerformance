namespace EngineeringPerformance.Application.Dictionaries;

public sealed record DictionaryItemDto(
    int Id,
    string Name,
    string? Code,
    string? Description,
    int? ParentId,
    int? UnitId);

/// <summary>
/// Все 8 справочников страницы «Справочники» + регистр норм ИИС (кэшируется на фронте).
/// </summary>
public sealed record DictionariesAllDto(
    IReadOnlyList<DictionaryItemDto> PhysicalVolumeTypes,
    IReadOnlyList<DictionaryItemDto> PhysicalVolumes,
    IReadOnlyList<DictionaryItemDto> Equipments,
    IReadOnlyList<DictionaryItemDto> LaborIntensities,
    IReadOnlyList<DictionaryItemDto> IicWorkDirections,
    IReadOnlyList<DictionaryItemDto> IicWorkDirectionNorms,
    IReadOnlyList<DictionaryItemDto> UnitsOfMeasurement,
    IReadOnlyList<DictionaryItemDto> ProjectCharacteristics);
