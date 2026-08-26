using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Демонстрационные данные — паритет UI-компонентов вкладки «Отчёт»
// (PlanFactSummary.tsx / ZScale.tsx / TopPerformersPanel.tsx / PhysicalVolumeGroupsTable.tsx).

var planColumn = new PdfPlanFactColumn(
    VersionLabel: "Основная (план) v2",
    Hours: "184 500",
    ZScore: "0.35",
    Percentile: "63-й",
    Interpretation: "Хорошо");

var factColumn = new PdfPlanFactColumn(
    VersionLabel: "Основная (факт) v5",
    Hours: "201 220",
    ZScore: "-0.62",
    Percentile: "27-й",
    Interpretation: "Отлично");

var deltaCaption = "Δ трудоёмкость: +16 720 ч относительно плана";

string[] rankHeaders = ["#", "Направление ИИЦ", "Код", "Часы", "Z", "Оценка"];

var best = new[]
{
    new HierarchicalPdfRow(0, ["1", "Электроснабжение 0.4-10 кВ", "ЭС-01", "18 420", "-1.82", "Отлично"], "Отлично"),
    new HierarchicalPdfRow(0, ["2", "Автоматизация технологических процессов", "АТП-04", "12 050", "-1.44", "Отлично"], "Отлично"),
    new HierarchicalPdfRow(0, ["3", "Водоснабжение и водоотведение", "ВК-02", "9 870", "-1.10", "Хорошо"], "Хорошо"),
    new HierarchicalPdfRow(0, ["4", "Отопление, вентиляция, кондиционирование", "ОВ-03", "14 300", "-0.95", "Хорошо"], "Хорошо"),
    new HierarchicalPdfRow(0, ["5", "Связь и сигнализация", "СС-06", "6 540", "-0.71", "Хорошо"], "Хорошо"),
};

var worst = new[]
{
    new HierarchicalPdfRow(0, ["1", "Генеральный план и транспорт", "ГП-01", "22 180", "2.14", "Требует внимания"], "Требует внимания"),
    new HierarchicalPdfRow(0, ["2", "Строительные решения", "АС-02", "31 700", "1.86", "Требует внимания"], "Требует внимания"),
    new HierarchicalPdfRow(0, ["3", "Технология производства", "ТХ-01", "27 990", "1.52", "Удовлетворительно"], "Удовлетворительно"),
    new HierarchicalPdfRow(0, ["4", "Пожарная безопасность", "ПБ-05", "8 260", "1.20", "Удовлетворительно"], "Удовлетворительно"),
    new HierarchicalPdfRow(0, ["5", "Металлические конструкции", "КМ-03", "15 410", "0.98", "Удовлетворительно"], "Удовлетворительно"),
};

var volumeRows = new[]
{
    new HierarchicalPdfRow(0, ["ГП-01", "Генеральный план и транспорт", "га", "12.40", "22 180", "0.045", "0.061", "2.14", "Требует внимания"], "Требует внимания"),
    new HierarchicalPdfRow(0, ["АС-02", "Строительные решения", "м²", "48 500.00", "31 700", "0.654", "0.812", "1.86", "Требует внимания"], "Требует внимания"),
    new HierarchicalPdfRow(0, ["ТХ-01", "Технология производства", "т/сут", "1 250.00", "27 990", "22.39", "26.10", "1.52", "Удовлетворительно"], "Удовлетворительно"),
    new HierarchicalPdfRow(0, ["ЭС-01", "Электроснабжение 0.4-10 кВ", "км", "84.20", "18 420", "0.219", "0.198", "-1.82", "Отлично"], "Отлично"),
    new HierarchicalPdfRow(0, ["АТП-04", "Автоматизация технологических процессов", "точка", "612.00", "12 050", "19.69", "17.92", "-1.44", "Отлично"], "Отлично"),
    new HierarchicalPdfRow(0, ["ВК-02", "Водоснабжение и водоотведение", "км", "36.80", "9 870", "0.268", "0.245", "-1.10", "Хорошо"], "Хорошо"),
};

var teaserKpis = new[]
{
    new ReportKpiItem("Часы (факт/план)", "201 220"),
    new ReportKpiItem("Взвеш. Z", "-0.62"),
    new ReportKpiItem("Оценка", "Отлично"),
};

Action<IContainer> content = c => c.Padding(20).Column(col =>
{
    col.Spacing(4);
    col.Item().Element(e => QuestPdfReportComposer.ComposePlanFactSummary(e, planColumn, factColumn, deltaCaption));
    col.Item().PaddingTop(10).Element(e => QuestPdfReportComposer.ComposeZScoreScaleLegend(e, 0.35m, -0.62m));

    col.Item().PaddingTop(10).Text("Топ-5 направлений ИИЦ по трудоёмкости");
    col.Item().Row(row =>
    {
        row.Spacing(10);
        row.RelativeItem().Element(e => QuestPdfReportComposer.ComposeTable(
            e, "Лучшие (наименьший Z — опережение нормы)", rankHeaders,
            best.Select(r => r.Cells), interpretationColumnIndex: 5));
        row.RelativeItem().Element(e => QuestPdfReportComposer.ComposeTable(
            e, "Худшие (наибольший Z — перерасход)", rankHeaders,
            worst.Select(r => r.Cells), interpretationColumnIndex: 5));
    });
});

Action<IContainer> volumeContent = c => c.Padding(20).Column(col =>
{
    col.Item().Element(e => QuestPdfReportComposer.ComposeHierarchicalTable(
        e, "Все группы физобъёмов ИИЦ",
        ["Код", "Направление ИИЦ", "Ед. изм.", "Факт. объём", "Часы", "Произв. (факт)", "Норма ИИЦ", "Z", "Оценка"],
        volumeRows,
        interpretationColumnIndex: 8));
});

var document = Document.Create(documentContainer =>
{
    documentContainer.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(0);
        page.Background(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily(ReportVisualTheme.EmbeddedFontName));
        page.Content().Element(content);
    });
    documentContainer.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(0);
        page.Background(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily(ReportVisualTheme.EmbeddedFontName));
        page.Content().Element(volumeContent);
    });
});

var outputDir = args.Length > 0 ? args[0] : "/tmp/agent-browser";
Directory.CreateDirectory(outputDir);
var pdfPath = Path.Combine(outputDir, "report-preview.pdf");
File.WriteAllBytes(pdfPath, document.GeneratePdf());

var images = document.GenerateImages().ToList();
for (var i = 0; i < images.Count; i++)
{
    var pngPath = Path.Combine(outputDir, $"report-preview-p{i + 1}.png");
    File.WriteAllBytes(pngPath, images[i]);
    Console.WriteLine($"OK: {pngPath} ({images[i].Length} bytes)");
}
Console.WriteLine($"OK: {pdfPath}");
