namespace EngineeringPerformance.Application.Analytics;

public sealed record ProjectForecastReportDto(
    string ProjectName,
    string ProjectCode,
    string VersionName,
    IReadOnlyDictionary<string, string> ProjectCharacteristics,
    IReadOnlyList<ForecastIicVolumeRowDto> PlannedVolumes,
    IReadOnlyList<ForecastScenarioDto> Scenarios,
    IReadOnlyList<TopDisciplineDto> Top3Disciplines,
    IReadOnlyList<ScenarioInterpretationDto> Interpretations);

/// <summary>
/// Строка дерева групп физ. объёмов (ИИС) для прогноза: нормы на дату версии анализа, плановый объём и расчёт трудозатрат по сценариям.
/// </summary>
public sealed record ForecastIicVolumeRowDto(
    int WorkDirectionId,
    int? ParentId,
    string Name,
    string? UnitName,
    decimal? NormProductivity,
    decimal? NormEstimate,
    decimal? NormStandardDeviation,
    decimal PlannedVolume,
    decimal OptimisticHours,
    decimal BaseHours,
    decimal PessimisticHours,
    string Range,
    /// <summary>Полный путь «родитель / … / узел» для плоских таблиц сценариев.</summary>
    string HierarchyPath);

public sealed record ForecastScenarioDto(
    string ScenarioName,
    decimal ZScore,
    decimal TotalLaborHours,
    IReadOnlyList<ForecastIicVolumeRowDto> Disciplines);

public sealed record TopDisciplineDto(
    int Rank,
    string DisciplineName,
    decimal BaseHours);

public sealed record ScenarioInterpretationDto(
    string ScenarioName,
    decimal LaborHours,
    decimal PercentOfBase,
    string Interpretation);

public sealed record ProjectForecastAssessmentVolumesDto(
    IReadOnlyDictionary<int, List<ProjectVersionPhysicalVolumeDto>> PhysicalVolumesByDirection,
    IReadOnlyDictionary<int, List<ProjectVersionPhysicalVolumeDto>> EquipmentVolumesByDirection);

/// <summary>Параметры расчёта прогноза: метод и Z-оценки сценариев.</summary>
public sealed record ForecastCalculationOptionsDto(
    string Method = "regression",
    decimal ZOptimistic = -0.5m,
    decimal ZBase = 0m,
    decimal ZPessimistic = 0.5m);

/// <summary>Тело POST preview/calculate: плановые объёмы и параметры расчёта.</summary>
public sealed record ForecastPreviewRequestDto(
    IReadOnlyDictionary<int, decimal>? NodePlannedTotals = null,
    ForecastCalculationOptionsDto? Calculation = null);

/// <summary>Тело PUT planned-volumes: список плановых объёмов по направлениям ИИС.</summary>
public sealed record SaveForecastPlannedVolumesRequestDto(
    IReadOnlyList<ForecastPlannedVolumeItemDto> Items);

public sealed record ForecastPlannedVolumeItemDto(
    int WorkDirectionId,
    decimal Quantity);
