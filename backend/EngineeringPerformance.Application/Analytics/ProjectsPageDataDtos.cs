namespace EngineeringPerformance.Application.Analytics;

/// <summary>Объединённый ответ для 9 гридов ProjectsPage (один HTTP вместо 11+ round-trips).</summary>
public sealed record ProjectsPageDataDto(
    ProjectsPageForecastGridsDto Forecast,
    ProjectsPageFactGridsDto Fact,
    ProjectsPageCompareGridsDto? Compare);

public sealed record ProjectsPageForecastGridsDto(
    IReadOnlyList<ProjectsPageForecastSummaryRowDto> Summary,
    IReadOnlyList<ProjectsPageFacilityRowDto> Facility,
    IReadOnlyList<ProjectsPageDetailRowDto> Detail,
    ForecastScenarioTotalsDto GrandTotals);

public sealed record ForecastScenarioTotalsDto(
    decimal BaseHours,
    decimal OptimisticHours,
    decimal PessimisticHours);

/// <summary>Строка вкладки «Итого» прогноза (паритет WPF ForecastPerformanceItem).</summary>
public sealed record ProjectsPageForecastSummaryRowDto(
    int WorkDirectionId,
    string DisplayName,
    string NormUnitName,
    decimal? FactPhysicalVolume,
    decimal? NormProductivity,
    decimal? BaseHours,
    decimal? OptimisticHours,
    decimal? PessimisticHours,
    bool IsTotal,
    int Level,
    int? ParentId = null,
    string Code = "",
    IReadOnlyList<string>? Hierarchy = null,
    /// <summary>ФО на строках итогов по единицам измерения: «число (ЕИ); …».</summary>
    string? FactPhysicalVolumeDisplay = null,
    /// <summary>Поэлементный физобъём (own до remap оценки).</summary>
    decimal? PhysicalVolume = null,
    string? PhysicalVolumeDisplay = null);

public sealed record ProjectsPageFactGridsDto(
    IReadOnlyList<PerformanceRowDto> Performance,
    IReadOnlyList<ProjectsPageFacilityRowDto> Facility,
    IReadOnlyList<ProjectsPageDetailRowDto> Detail);

public sealed record ProjectsPageCompareGridsDto(
    IReadOnlyList<VersionCompareRowDto> Summary,
    IReadOnlyList<VersionCompareFacilityRowDto> Facility,
    IReadOnlyList<VersionCompareDetailRowDto> Detail);

public sealed record ProjectsPageFacilityRowDto(
    string FacilityCode,
    string FacilityName,
    decimal? LaborHours,
    decimal? ZScore,
    string Interpretation,
    decimal? BaseHours = null,
    decimal? OptimisticHours = null,
    decimal? PessimisticHours = null);

public sealed record ProjectsPageDetailRowDto(
    int? RowId,
    string FacilityCode,
    string FacilityName,
    string DocumentSet,
    string CatalogCode,
    string CatalogName,
    string WorkId,
    string WorkName,
    string IicGroupName,
    string IicUnitName,
    decimal Quantity,
    decimal? BaseHours,
    decimal? OptimisticHours,
    decimal? PessimisticHours,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? ZScore,
    string Interpretation,
    bool IsEquipment,
    string BindingType = "",
    string CatalogId = "",
    string Tag = "",
    string ItemName = "",
    string WorkComment = "",
    string EstimationBasis = "",
    string PvCode = "",
    string PvName = "",
    string IsAssessment = "",
    decimal? Coefficient = null,
    decimal? NormProductivity = null,
    decimal? NormStdDev = null,
    int? ClassifierId = null,
    int? NormId = null,
    int? CatalogVolumeId = null,
    int? CatalogEquipmentId = null,
    string CatalogResource = "");
