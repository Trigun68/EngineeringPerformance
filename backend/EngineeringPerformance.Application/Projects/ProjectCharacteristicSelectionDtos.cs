namespace EngineeringPerformance.Application.Projects;

public sealed record ProjectCharacteristicSelectionDto(
    int CharacteristicId,
    string CharacteristicName,
    int? SelectedOptionId,
    string? SelectedOptionName,
    decimal? SelectedValuePercent,
    IReadOnlyList<ProjectCharacteristicOptionDto> Options);

public sealed record ProjectCharacteristicOptionDto(
    int Id,
    string Name,
    decimal? ValuePercent);

public sealed record ProjectCharacteristicSelectionWriteDto(
    int CharacteristicId,
    int OptionId);

public sealed record ProjectCharacteristicSelectionsUpdateRequest(
    IReadOnlyList<ProjectCharacteristicSelectionWriteDto> Selections);
