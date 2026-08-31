namespace EngineeringPerformance.Application.Dictionaries;

public sealed record DictionaryItemDto(
    int Id,
    string Name,
    string? Code,
    string? Description,
    int? ParentId,
    int? UnitId);

/// <summary>
/// Справочники страницы «Справочники» + регистр норм ИИС (кэшируется на фронте).
/// PhysicalVolumes — объединение KFO+BER (обратная совместимость); для FK БЕР→КФО
/// используйте PhysicalVolumesKfo (ID в таблицах не пересекаются по смыслу, но совпадают численно).
/// </summary>
public sealed record DictionariesAllDto(
    IReadOnlyList<DictionaryItemDto> PhysicalVolumeTypes,
    IReadOnlyList<DictionaryItemDto> PhysicalVolumes,
    IReadOnlyList<DictionaryItemDto> PhysicalVolumesKfo,
    IReadOnlyList<DictionaryItemDto> PhysicalVolumesBer,
    IReadOnlyList<DictionaryItemDto> Equipments,
    IReadOnlyList<DictionaryItemDto> LaborIntensities,
    IReadOnlyList<DictionaryItemDto> IicWorkDirections,
    IReadOnlyList<DictionaryItemDto> IicWorkDirectionNorms,
    IReadOnlyList<DictionaryItemDto> UnitsOfMeasurement,
    IReadOnlyList<DictionaryItemDto> ProjectCharacteristics);
