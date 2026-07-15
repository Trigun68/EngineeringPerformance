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

        var pdf = QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(QuestPdfReportComposer.ComposeZScoreScaleLegend);
            col.Item().PaddingTop(6).Element(c =>
                QuestPdfReportComposer.ComposeHierarchicalTable(
                    c, "Тест иерархии",
                    ["Направление", "Часы", "Оценка"],
                    rows,
                    interpretationColumnIndex: 2));
        }, "Smoke PDF", "meta");

        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');
        pdf[1].Should().Be((byte)'P');
        pdf[2].Should().Be((byte)'D');
        pdf[3].Should().Be((byte)'F');

        QuestPdfReportComposer.IndentLabel("Лист", 1).Should().Contain("└");
        ReportVisualTheme.AccentA.Should().Be("#3498DB");
        ReportVisualTheme.InterpretationColor("Требует внимания").Should().Be(ReportVisualTheme.Negative);
        ReportVisualTheme.InterpretationColor("Хорошо").Should().Be(ReportVisualTheme.Positive);
    }
}
