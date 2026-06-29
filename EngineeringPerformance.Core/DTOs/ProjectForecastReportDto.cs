namespace EngineeringPerformance.Core.DTOs;

public sealed record ProjectForecastReportDto(
    string ProjectName,
    string ProjectCode,
    string VersionName,
    IReadOnlyDictionary<string, string> ProjectCharacteristics,
    IReadOnlyList<ForecastIicVolumeRowDto> PlannedVolumes,
    IReadOnlyList<ForecastScenarioDto> Scenarios,
    IReadOnlyList<TopDisciplineDto> Top3Disciplines,
    IReadOnlyList<ScenarioInterpretationDto> Interpretations);
