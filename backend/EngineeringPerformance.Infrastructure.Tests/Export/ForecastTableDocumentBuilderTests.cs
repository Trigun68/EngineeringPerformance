using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class ForecastTableDocumentBuilderTests
{
    [Fact]
    public void Summary_columns_are_opt_then_base_then_pes()
    {
        var document = ForecastTableDocumentBuilder.BuildSummary(
            [
                new ProjectsPageForecastSummaryRowDto(
                    1, "Направление", "ч", 10m, 2m, 100m, 80m, 120m, false, 0, Code: "A1"),
            ],
            "Прогноз трудозатрат: P (v1)");

        document.Title.Should().StartWith("Прогноз трудозатрат");
        document.Sections.Should().ContainSingle();
        document.Sections[0].Columns.Select(c => c.Header).Should().Equal(
            "Шифр",
            "Направление",
            "Факт ФО",
            "Норма производительности",
            "Оптимистический, ч",
            "Базовый, ч",
            "Пессимистический, ч");

        var cells = document.Sections[0].Rows[0].Cells;
        cells[0].Text.Should().Be("A1");
        cells[4].Number.Should().Be(80m);
        cells[4].Tone.Should().Be(ForecastHourTone.Optimistic);
        cells[5].Number.Should().Be(100m);
        cells[6].Number.Should().Be(120m);
        cells[6].Tone.Should().Be(ForecastHourTone.Pessimistic);
    }

    [Fact]
    public void Facility_puts_top10_then_all_objects()
    {
        var rows = Enumerable.Range(1, 12)
            .Select(i => new ProjectsPageFacilityRowDto(
                $"F{i:00}",
                $"Объект {i}",
                LaborHours: i * 10m,
                ZScore: null,
                Interpretation: string.Empty,
                BaseHours: i * 10m,
                OptimisticHours: i * 8m,
                PessimisticHours: i * 12m))
            .ToList();

        var document = ForecastTableDocumentBuilder.BuildFacility(rows, "Прогноз по объектам: P (v1)");
        document.Sections.Should().HaveCount(2);
        document.Sections[0].Title.Should().Be("Топ-10 объектов");
        document.Sections[1].Title.Should().Be("Все объекты");
        document.Sections[0].Rows.Should().HaveCount(10);
        document.Sections[0].Rows[0].Cells[0].Text.Should().Be("F12");
        document.Sections[0].Columns.Select(c => c.Header).Should().Equal(
            "Шифр объекта",
            "Наименование объекта",
            "Оптимистический",
            "Базовый",
            "Пессимистический");
        document.Sections[0].Rows[0].Cells[2].Number.Should().Be(96m);
        document.Sections[0].Rows[0].Cells[2].Tone.Should().Be(ForecastHourTone.Optimistic);
        document.Sections[0].Rows[0].Cells[3].Number.Should().Be(120m);
        document.Sections[0].Rows[0].Cells[4].Number.Should().Be(144m);
        document.Sections[0].Rows[0].Cells[4].Tone.Should().Be(ForecastHourTone.Pessimistic);
        document.Sections[1].Rows.Should().HaveCount(13);
        document.Sections[1].Rows[^1].IsTotal.Should().BeTrue();
        document.Sections[1].Rows[^1].Cells[0].Text.Should().Be("ИТОГО");
        document.Sections[1].Rows[^1].Cells[2].Number.Should().Be(624m);
        document.Sections[1].Rows[^1].Cells[3].Number.Should().Be(780m);
        document.Sections[1].Rows[^1].Cells[4].Number.Should().Be(936m);
    }

    [Fact]
    public void Detail_has_22_columns_and_pdf_is_not_empty()
    {
        var row = new ProjectsPageDetailRowDto(
            1,
            "F01",
            "Объект",
            "Комплект",
            "CAT",
            "Позиция",
            "W1",
            "Работа",
            "IIC",
            "ч",
            2m,
            10m,
            8m,
            12m,
            null,
            null,
            null,
            string.Empty,
            false,
            ItemName: "Наименование",
            PvName: "ФО");

        var document = ForecastTableDocumentBuilder.BuildDetail([row], "Детализация прогноза: P (v1)");
        document.Sections[0].Columns.Should().HaveCount(22);
        document.Sections[0].Rows.Should().ContainSingle();
        document.Sections[0].Columns[^3].Header.Should().Be("Оптимистический");
        document.Sections[0].Columns[^2].Header.Should().Be("Базовый");
        document.Sections[0].Columns[^1].Header.Should().Be("Пессимистический");
        document.Sections[0].Rows[0].Cells[^3].Number.Should().Be(8m);
        document.Sections[0].Rows[0].Cells[^2].Number.Should().Be(10m);
        document.Sections[0].Rows[0].Cells[^1].Number.Should().Be(12m);

        var pdf = ForecastTableRenderers.ToPdf(document);
        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');
        pdf[1].Should().Be((byte)'P');

        var excel = ForecastTableRenderers.ToExcel(document);
        using var stream = new MemoryStream(excel);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        sheet.Cell(1, 1).GetString().Should().Be("Детализация прогноза: P (v1)");
        sheet.Cell(3, 1).GetString().Should().Be("Шифр объекта");
        sheet.Cell(3, 22).GetString().Should().Be("Пессимистический");
        sheet.Cell(4, 1).GetString().Should().Be("F01");
    }
}
