using ClosedXML.Excel;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class DirectoryExportServiceTests
{
    [Fact]
    public void ExportToExcel_ReturnsWorkbookWithHeadersAndRows()
    {
        var service = new DirectoryExportService();
        var request = new DirectoryExportRequest(
            TabName: "KFO",
            Headers: ["Код", "Наименование"],
            Rows:
            [
                ["01", "Раздел 1"],
                ["01-01", "Подраздел"],
            ]);

        var bytes = service.ExportToExcel(request);

        bytes.Should().NotBeNullOrEmpty();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("KFO");
        ws.Cell(1, 1).GetString().Should().Be("Код");
        ws.Cell(1, 2).GetString().Should().Be("Наименование");
        ws.Cell(2, 1).GetString().Should().Be("01");
        ws.Cell(3, 2).GetString().Should().Be("Подраздел");
    }

    [Fact]
    public void ExportToExcel_ThrowsWhenNoHeaders()
    {
        var service = new DirectoryExportService();
        var request = new DirectoryExportRequest("Empty", [], []);

        var act = () => service.ExportToExcel(request);

        act.Should().Throw<ArgumentException>();
    }
}
