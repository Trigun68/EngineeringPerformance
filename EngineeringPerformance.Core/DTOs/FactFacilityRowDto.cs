namespace EngineeringPerformance.Core.DTOs;

// Строка таблицы «По объектам» вкладки «Оценка факта» страницы «Проекты».
// Z-score — взвешенная по часам факта оценка на уровне объекта (FacilityCode),
// получаемая из связки ProjectLaborIntensity.DocumentationCode ↔ ProjectPhysicalVolume.FacilityCode.
public sealed record FactFacilityRowDto(
    string Code,
    string Name,
    decimal? FactLaborHours,
    decimal? ZScore,
    string Percentile,
    string Interpretation);
