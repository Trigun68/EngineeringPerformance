namespace EngineeringPerformance.Application.Analytics;

/// <summary>Объединённый ответ для 9 гридов ProjectsPage (один HTTP вместо 11+ round-trips).</summary>
public sealed record ProjectsPageDataDto(
    ProjectsPageForecastGridsDto Forecast,
    ProjectsPageFactGridsDto Fact,
    ProjectsPageCompareGridsDto? Compare);

public sealed record ProjectsPageForecastGridsDto(
    IReadOnlyList<ForecastIicVolumeRowDto> Summary,
    IReadOnlyList<ProjectsPageFacilityRowDto> Facility,
    IReadOnlyList<ProjectsPageDetailRowDto> Detail);

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
    string Interpretation);

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
    bool IsEquipment);
