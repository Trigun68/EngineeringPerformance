using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Unit-тесты экспорта ProjectsPage: данные только через <see cref="IProjectsPageDataFetcher"/>.
/// </summary>
public sealed class ExportServiceTests
{
    private static ProjectsPageDataDto CreateSampleData() =>
        new(
            new ProjectsPageForecastGridsDto(
                Summary:
                [
                    new ProjectsPageForecastSummaryRowDto(
                        1, "Направление А", "шт",
                        80m, 10m, 1000m, 950m, 1050m,
                        false, 0),
                ],
                Facility: [],
                Detail: [],
                GrandTotals: new ForecastScenarioTotalsDto(1000m, 950m, 1050m)),
            new ProjectsPageFactGridsDto(
                Performance:
                [
                    new PerformanceRowDto(
                        1, "Направление А", "DIR-A", null, 0, "шт",
                        80m, 800m, 10m, 2.3m, 10m, 2.3m, 0.5m, 0.1m,
                        PerformanceInterpretation.Satisfactory, 800m),
                ],
                Facility: [],
                Detail:
                [
                    new ProjectsPageDetailRowDto(
                        1, "F1", "Объект 1", "DS-1", "01-01-001-01", "Работа 1",
                        "W1", "Монтаж", "DIR-A", "шт", 10m,
                        100m, 95m, 105m, 50m, null, null, "", false),
                    new ProjectsPageDetailRowDto(
                        2, "F1", "Объект 1", "DS-1", "EQ-001", "Насос",
                        "W2", "Оборудование", "DIR-A", "шт", 2m,
                        20m, 18m, 22m, 15m, null, null, "", true),
                ]),
            Compare: null);

    [Fact]
    public async Task ExportToExcelAsync_ReturnsNonEmptyBytes_AndUsesDataFetcher()
    {
        var fetcher = new StubProjectsPageDataFetcher(CreateSampleData());
        var service = new ProjectExportService(fetcher, new StubForecastDetailSummaryTableContext());

        var bytes = await service.ExportToExcelAsync(1, 10);

        bytes.Should().NotBeNullOrEmpty();
        fetcher.CallCount.Should().Be(1);
        fetcher.LastProjectId.Should().Be(1);
        fetcher.LastVersionId.Should().Be(10);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.Should().Contain(ws => ws.Name == "Forecast");
        workbook.Worksheets.Should().Contain(ws => ws.Name == "PPV");
        workbook.Worksheets.Should().Contain(ws => ws.Name == "Equipment");
        workbook.Worksheets.Should().Contain(ws => ws.Name == "LI");
    }

    [Fact]
    public async Task ExportToPdfAsync_ReturnsNonEmptyBytes_AndUsesDataFetcher()
    {
        var fetcher = new StubProjectsPageDataFetcher(CreateSampleData());
        var service = new ProjectExportService(fetcher, new StubForecastDetailSummaryTableContext());

        var bytes = await service.ExportToPdfAsync(2, 20, compareVersionId: 15);

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x25); // PDF magic '%'
        fetcher.CallCount.Should().Be(1);
        fetcher.LastCompareVersionId.Should().Be(15);
    }

    private sealed class StubProjectsPageDataFetcher(ProjectsPageDataDto data) : IProjectsPageDataFetcher
    {
        public int CallCount { get; private set; }
        public int LastProjectId { get; private set; }
        public int LastVersionId { get; private set; }
        public int? LastCompareVersionId { get; private set; }

        public Task<ProjectsPageDataDto> GetProjectsPageDataAsync(
            int projectId,
            int versionId,
            int? compareVersionId = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastProjectId = projectId;
            LastVersionId = versionId;
            LastCompareVersionId = compareVersionId;
            return Task.FromResult(data);
        }

        public Task<ProjectsPageDataEncodedDto> GetEncodedPageDataAsync(
            int projectId,
            int versionId,
            int? compareVersionId = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastProjectId = projectId;
            LastVersionId = versionId;
            LastCompareVersionId = compareVersionId;
            return Task.FromResult(ProjectsPageDataEncodedDto.From(data).WithoutDetailRows());
        }

        public Task<ProjectsPageDetailsEncodedDto> GetEncodedPageDetailsAsync(
            int projectId,
            int versionId,
            int? compareVersionId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProjectsPageDetailsEncodedDto(
                DetailGridEncoder.Encode(data.Forecast.Detail),
                DetailGridEncoder.Encode(data.Fact.Detail),
                data.Compare is null ? null : CompareDetailEncoder.Encode(data.Compare.Detail)));
    }

    private sealed class StubForecastDetailSummaryTableContext : IForecastDetailSummaryTableContext
    {
        public Task<(string ProjectCode, string VersionName, IReadOnlyList<IicDirectionNodeDto> Directions)> LoadAsync(
            int projectId,
            int versionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(string, string, IReadOnlyList<IicDirectionNodeDto>)>(
                ("P-01", "Plan v1", Array.Empty<IicDirectionNodeDto>()));
    }
}
