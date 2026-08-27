using System.Globalization;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Export;

public sealed class AnalyticsExportService : IAnalyticsExportService
{
    private readonly IProjectVersionAnalysisService _analysisService;
    private readonly IProjectForecastService _forecastService;
    private readonly IVisualReportService _visualReports;

    public AnalyticsExportService(
        IProjectVersionAnalysisService analysisService,
        IProjectForecastService forecastService,
        IVisualReportService visualReports)
    {
        _analysisService = analysisService;
        _forecastService = forecastService;
        _visualReports = visualReports;
    }

    public async Task<byte[]> ExportAnalysisExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysisService.GenerateReportAsync(projectId, versionId, cancellationToken);
        var document = FactTableDocumentBuilder.BuildAnalysis(report) with
        {
            HeaderLines = TabularReportBanner.Lines(report.ProjectCode ?? string.Empty, report.ProjectName, report.VersionName),
        };
        return ForecastTableRenderers.ToExcel(document);
    }

    public Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _visualReports.ExportAnalysisPdfAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportForecastExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _forecastService.GenerateForecastAsync(projectId, versionId, cancellationToken: cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Прогноз");
        sheet.Cell(1, 1).Value = $"Прогноз трудозатрат: {report.ProjectName} ({report.VersionName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, 6).Merge();

        var headers = new[] { "Сценарий", "Z-Score", "Трудоёмкость, ч", "Направление", "Ед. изм.", "План. объём" };
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Cell(2, c + 1).Style.Font.Bold = true;
            sheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        foreach (var scenario in report.Scenarios)
        {
            sheet.Cell(row, 1).Value = scenario.ScenarioName;
            sheet.Cell(row, 2).Value = scenario.ZScore;
            sheet.Cell(row, 3).Value = scenario.TotalLaborHours;
            row++;
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Плановые объёмы";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        var volHeaders = new[] { "Направление", "Ед. изм.", "План. объём", "Базовый", "Оптимистический", "Пессимистический" };
        for (var c = 0; c < volHeaders.Length; c++)
        {
            sheet.Cell(row, c + 1).Value = volHeaders[c];
            sheet.Cell(row, c + 1).Style.Font.Bold = true;
            sheet.Cell(row, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(row, c + 1).Style.Font.FontColor = XLColor.White;
        }
        row++;
        foreach (var volume in report.PlannedVolumes)
        {
            sheet.Cell(row, 1).Value = volume.HierarchyPath;
            sheet.Cell(row, 2).Value = volume.UnitName ?? string.Empty;
            sheet.Cell(row, 3).Value = volume.PlannedVolume;
            sheet.Cell(row, 4).Value = volume.BaseHours;
            sheet.Cell(row, 5).Value = volume.OptimisticHours;
            sheet.Cell(row, 6).Value = volume.PessimisticHours;
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public Task<byte[]> ExportForecastPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _visualReports.ExportForecastSummaryPdfAsync(projectId, versionId, cancellationToken);
}
