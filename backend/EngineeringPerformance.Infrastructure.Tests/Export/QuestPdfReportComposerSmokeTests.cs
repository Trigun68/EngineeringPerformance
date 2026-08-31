using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;
using QuestPDF.Fluent;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class QuestPdfReportComposerSmokeTests
{
    [Fact]
    public void EmbeddedFont_PathExists_WhenPackaged()
    {
        // В Docker/CI шрифт копируется в output; локально может отсутствовать в test bin.
        var path = QuestPdfReportComposer.FontPathForTests;
        if (path is null)
            return;

        File.Exists(path).Should().BeTrue();
        Path.GetFileName(path).Should().Be("DejaVuSans.ttf");
    }

    [Fact]
    public void BuildLandscapeReport_ReturnsPdfMagicAndHierarchyIndent()
    {
        var rows = new[]
        {
            new HierarchicalPdfRow(0, ["Корень", "10", "Отлично"], "Отлично"),
            new HierarchicalPdfRow(1, ["Лист", "5", "Удовлетворительно"], "Удовлетворительно"),
        };

        var kpis = new[] { new ReportKpiItem("Часы", "10") };

        var pdf = QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            col.Item().PaddingTop(6).Element(c => QuestPdfReportComposer.ComposeZScoreScaleLegend(c, 0.7m));
            col.Item().PaddingTop(6).Element(c =>
                QuestPdfReportComposer.ComposeHierarchicalTable(
                    c, "Тест иерархии",
                    ["Направление", "Часы", "Оценка"],
                    rows,
                    interpretationColumnIndex: 2));
        }, PdfReportKind.Fact, "Smoke PDF", "Тестовый проект", "v1", kpis);

        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');
        pdf[1].Should().Be((byte)'P');
        pdf[2].Should().Be((byte)'D');
        pdf[3].Should().Be((byte)'F');

        // Отступ иерархии — пробелы (визуальный маркер-линия рисуется только внутри PDF-ячейки).
        QuestPdfReportComposer.IndentLabel("Лист", 1).Should().StartWith(new string(' ', ReportVisualTheme.HierarchyIndentSpaces));
        QuestPdfReportComposer.IndentLabel("Лист", 1).Should().EndWith("Лист");
        QuestPdfReportComposer.IndentLabel("Корень", 0).Should().Be("Корень");

        ReportVisualTheme.AccentA.Should().Be("#2E86C1");
        ReportVisualTheme.InterpretationColor("Требует внимания").Should().Be(ReportVisualTheme.Negative);
        ReportVisualTheme.InterpretationColor("Хорошо").Should().Be(ReportVisualTheme.Positive);
    }

    [Fact]
    public void BuildLandscapeReport_CoverSubValue_StillProducesPdf()
    {
        var coverKpis = new[]
        {
            new ReportKpiItem(PdfScenarioLabels.Optimistic, "10,0 ч", "1 000 ₽"),
            new ReportKpiItem(PdfScenarioLabels.Base, "20,0 ч", "2 000 ₽"),
            new ReportKpiItem(PdfScenarioLabels.Pessimistic, "30,0 ч", "3 000 ₽"),
        };

        var pdf = QuestPdfReportComposer.BuildLandscapeReport(
            col => col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, PdfScenarioKpis.HoursOnly(10, 20, 30))),
            PdfReportKind.Forecast,
            "Smoke cover cost",
            "Тестовый проект",
            "v1",
            coverKpis);

        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');
    }

    [Fact]
    public void BuildPortraitReport_ProducesCoverAndContentPages()
    {
        var pdf = QuestPdfReportComposer.BuildPortraitReport(
            col => col.Item().Text("Содержимое отчёта"),
            PdfReportKind.Trend,
            "Smoke Portrait",
            "Тестовый проект",
            "v1");

        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');
    }
}
