namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сетевой вид ответа page-data: гриды «Детально» едут в упакованном виде (ISS-019).
/// Остальные гриды — десятки строк, паковать нечего. Внутренний <see cref="ProjectsPageDataDto"/>
/// не меняется: на нём построены экспорты в Excel/PDF.
/// </summary>
public sealed record ProjectsPageDataEncodedDto(
    ProjectsPageForecastGridsEncodedDto Forecast,
    ProjectsPageFactGridsEncodedDto Fact,
    ProjectsPageCompareGridsEncodedDto? Compare)
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
            data.Compare is null
                ? null
                : new ProjectsPageCompareGridsEncodedDto(
                    data.Compare.Summary,
                    data.Compare.Facility,
                    CompareDetailEncoder.Encode(data.Compare.Detail)));

    /// <summary>
    /// Первый экран вкладок «Итого» / «По объектам» не тащит десятки тысяч строк detail.
    /// Полный упакованный ответ остаётся в кэше; HTTP отдаёт пустые гриды с теми же колонками.
    /// </summary>
    public ProjectsPageDataEncodedDto WithoutDetailRows() =>
        new(
            Forecast with { Detail = DetailGridEncoder.Empty },
            Fact with { Detail = DetailGridEncoder.Empty },
            Compare is null ? null : Compare with { Detail = CompareDetailEncoder.Empty });
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

public sealed record ProjectsPageCompareGridsEncodedDto(
    IReadOnlyList<VersionCompareRowDto> Summary,
    IReadOnlyList<VersionCompareFacilityRowDto> Facility,
    EncodedGridDto Detail);

/// <summary>Только гриды «Детально» — догружаются при открытии вкладки.</summary>
public sealed record ProjectsPageDetailsEncodedDto(
    EncodedGridDto ForecastDetail,
    EncodedGridDto FactDetail,
    EncodedGridDto? CompareDetail);

/// <summary>Сетевой вид GET /compare: detail упакован (колоночный JSON).</summary>
public sealed record VersionCompareResultEncodedDto(
    IReadOnlyList<VersionCompareRowDto> Summary,
    IReadOnlyList<VersionCompareFacilityRowDto> Facility,
    EncodedGridDto Detail)
{
    public static VersionCompareResultEncodedDto From(VersionCompareResultDto data) =>
        new(data.Summary, data.Facility, CompareDetailEncoder.Encode(data.Detail));

    public static VersionCompareResultEncodedDto WithoutDetail(VersionCompareResultDto data) =>
        new(data.Summary, data.Facility, CompareDetailEncoder.Empty);
}
