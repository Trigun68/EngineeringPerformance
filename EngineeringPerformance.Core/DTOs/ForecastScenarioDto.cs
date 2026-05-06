namespace EngineeringPerformance.Core.DTOs;

public sealed record ForecastScenarioDto(
    string ScenarioName,
    decimal ZScore,
    decimal TotalLaborHours,
    IReadOnlyList<ForecastIicVolumeRowDto> Disciplines);
