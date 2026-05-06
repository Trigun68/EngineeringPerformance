namespace EngineeringPerformance.Core.DTOs;

public sealed record ScenarioInterpretationDto(
    string ScenarioName,
    decimal LaborHours,
    decimal PercentOfBase,
    string Interpretation);
