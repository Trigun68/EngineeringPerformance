using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

public sealed class AnalyticsExportService : IAnalyticsExportService
{
    private readonly IProjectVersionAnalysisService _analysisService;
    private readonly IProjectForecastService _forecastService;

    public AnalyticsExportService(
        IProjectVersionAnalysisService analysisService,
        IProjectForecastService forecastService)
    {
        _analysisService = analysisService;
        _forecastService = forecastService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportAnalysisExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysisService.GenerateReportAsync(projectId, versionId);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Analysis");

        sheet.Cell(1, 1).Value = "Project";
        sheet.Cell(1, 2).Value = report.ProjectName;
        sheet.Cell(2, 1).Value = "Version";
        sheet.Cell(2, 2).Value = report.VersionName;

        var headerRow = 4;
        sheet.Cell(headerRow, 1).Value = "Code";
        sheet.Cell(headerRow, 2).Value = "Name";
        sheet.Cell(headerRow, 3).Value = "Fact Labor Hours";
        sheet.Cell(headerRow, 4).Value = "Z-Score";
        sheet.Cell(headerRow, 5).Value = "Interpretation";

        var row = headerRow + 1;
        foreach (var direction in report.Directions)
        {
            sheet.Cell(row, 1).Value = direction.Code;
            sheet.Cell(row, 2).Value = direction.Name;
            sheet.Cell(row, 3).Value = direction.FactLaborHours;
            sheet.Cell(row, 4).Value = direction.ZScore;
            sheet.Cell(row, 5).Value = direction.Interpretation?.ToRussian() ?? string.Empty;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysisService.GenerateReportAsync(projectId, versionId);
        var summary = report.Summary;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text("Анализ версии проекта").SemiBold().FontSize(16);
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Проект: {report.ProjectName}");
                    column.Item().Text($"Версия: {report.VersionName}");
                    column.Item().Text($"Направлений IIC: {report.Directions.Count}");
                    column.Item().Text($"Суммарные факт. часы: {summary.TotalFactLaborHours?.ToString("N2") ?? "—"}");
                    column.Item().Text($"Взвешенный Z-score: {summary.WeightedZScore?.ToString("N4") ?? "—"} ({summary.WeightedZScorePercentile})");
                    column.Item().Text($"Интерпретация: {summary.WeightedZScoreInterpretation?.ToRussian() ?? "—"}");
                });
                page.Footer().AlignCenter().Text($"Сформировано {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportForecastExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _forecastService.GenerateForecastAsync(projectId, versionId);

        using var workbook = new XLWorkbook();

        var scenariosSheet = workbook.Worksheets.Add("Scenarios");
        scenariosSheet.Cell(1, 1).Value = "Scenario";
        scenariosSheet.Cell(1, 2).Value = "Z-Score";
        scenariosSheet.Cell(1, 3).Value = "Total Labor Hours";

        var scenarioRow = 2;
        foreach (var scenario in report.Scenarios)
        {
            scenariosSheet.Cell(scenarioRow, 1).Value = scenario.ScenarioName;
            scenariosSheet.Cell(scenarioRow, 2).Value = scenario.ZScore;
            scenariosSheet.Cell(scenarioRow, 3).Value = scenario.TotalLaborHours;
            scenarioRow++;
        }

        var volumesSheet = workbook.Worksheets.Add("Planned Volumes");
        volumesSheet.Cell(1, 1).Value = "Work Direction";
        volumesSheet.Cell(1, 2).Value = "Unit";
        volumesSheet.Cell(1, 3).Value = "Planned Volume";
        volumesSheet.Cell(1, 4).Value = "Base Hours";
        volumesSheet.Cell(1, 5).Value = "Optimistic Hours";
        volumesSheet.Cell(1, 6).Value = "Pessimistic Hours";

        var volumeRow = 2;
        foreach (var volume in report.PlannedVolumes)
        {
            volumesSheet.Cell(volumeRow, 1).Value = volume.HierarchyPath;
            volumesSheet.Cell(volumeRow, 2).Value = volume.UnitName ?? string.Empty;
            volumesSheet.Cell(volumeRow, 3).Value = volume.PlannedVolume;
            volumesSheet.Cell(volumeRow, 4).Value = volume.BaseHours;
            volumesSheet.Cell(volumeRow, 5).Value = volume.OptimisticHours;
            volumesSheet.Cell(volumeRow, 6).Value = volume.PessimisticHours;
            volumeRow++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportForecastPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _forecastService.GenerateForecastAsync(projectId, versionId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.Header().Text("Прогноз трудозатрат").SemiBold().FontSize(16);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Сценарий");
                        header.Cell().Element(CellStyle).Text("Z-Score");
                        header.Cell().Element(CellStyle).Text("Часы");
                    });

                    foreach (var scenario in report.Scenarios)
                    {
                        table.Cell().Element(CellStyle).Text(scenario.ScenarioName);
                        table.Cell().Element(CellStyle).Text(scenario.ZScore.ToString("N4"));
                        table.Cell().Element(CellStyle).Text(scenario.TotalLaborHours.ToString("N2"));
                    }
                });
                page.Footer().AlignCenter().Text($"{report.ProjectName} — {report.VersionName}");
            });
        });

        return document.GeneratePdf();

        static IContainer CellStyle(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);
    }
}
