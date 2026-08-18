namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сетевой вид ответа page-data: гриды «Детально» едут в упакованном виде (ISS-019).
/// Остальные гриды — десятки строк, паковать нечего. Внутренний <see cref="ProjectsPageDataDto"/>
/// не меняется: на нём построены экспорты в Excel/PDF.
/// </summary>
public sealed record ProjectsPageDataEncodedDto(
    ProjectsPageForecastGridsEncodedDto Forecast,
    ProjectsPageFactGridsEncodedDto Fact,
    ProjectsPageCompareGridsDto? Compare)
{
    public static ProjectsPageDataEncodedDto From(ProjectsPageDataDto data) =>
        new(
            new ProjectsPageForecastGridsEncodedDto(
                data.Forecast.Summary,
                data.Forecast.Facility,
                DetailGridEncoder.Encode(data.Forecast.Detail),
                data.Forecast.GrandTotals),
            new ProjectsPageFactGridsEncodedDto(
                data.Fact.Performance,
                data.Fact.Facility,
                DetailGridEncoder.Encode(data.Fact.Detail)),
            data.Compare);
}

public sealed record ProjectsPageForecastGridsEncodedDto(
    IReadOnlyList<ProjectsPageForecastSummaryRowDto> Summary,
    IReadOnlyList<ProjectsPageFacilityRowDto> Facility,
    EncodedGridDto Detail,
    ForecastScenarioTotalsDto GrandTotals);

public sealed record ProjectsPageFactGridsEncodedDto(
    IReadOnlyList<PerformanceRowDto> Performance,
    IReadOnlyList<ProjectsPageFacilityRowDto> Facility,
    EncodedGridDto Detail);
