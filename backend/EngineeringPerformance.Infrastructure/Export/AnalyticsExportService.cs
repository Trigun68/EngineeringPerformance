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
        var report = await _analysisService.GenerateReportAsync(projectId, versionId);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();

        var directionsSheet = workbook.Worksheets.Add("Направления IIC");
        directionsSheet.Cell(1, 1).Value =
            $"Анализ версии: {report.ProjectCode ?? report.ProjectName} ({report.VersionName})";
        directionsSheet.Cell(1, 1).Style.Font.Bold = true;
        directionsSheet.Cell(1, 1).Style.Font.FontSize = 13;
        directionsSheet.Range(1, 1, 1, 12).Merge();

        var dirHeaders = new[]
        {
            "Код", "Направление", "Ед.", "Факт ФО", "Факт ч",
            "План.Труд.Песс.", "План.Труд.Баз.", "План.Труд.Оптим.",
            "Факт произв.", "Норм. произв.", "Z", "Интерпретация",
        };
        for (var c = 0; c < dirHeaders.Length; c++)
        {
            directionsSheet.Cell(2, c + 1).Value = dirHeaders[c];
            directionsSheet.Cell(2, c + 1).Style.Font.Bold = true;
            directionsSheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            directionsSheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        foreach (var direction in report.Directions.OrderBy(d => d.Level).ThenBy(d => d.Code))
        {
            directionsSheet.Cell(row, 1).Value = direction.Code;
            directionsSheet.Cell(row, 2).Value = direction.Name;
            directionsSheet.Cell(row, 3).Value = direction.UnitName ?? string.Empty;
            if (direction.FactPhysicalVolume.HasValue)
                directionsSheet.Cell(row, 4).Value = direction.FactPhysicalVolume.Value;
            if (direction.FactLaborHours.HasValue)
                directionsSheet.Cell(row, 5).Value = direction.FactLaborHours.Value;
            if (direction.PessimisticHours.HasValue)
                directionsSheet.Cell(row, 6).Value = direction.PessimisticHours.Value;
            if (direction.BaseHours.HasValue)
                directionsSheet.Cell(row, 7).Value = direction.BaseHours.Value;
            if (direction.OptimisticHours.HasValue)
                directionsSheet.Cell(row, 8).Value = direction.OptimisticHours.Value;
            if (direction.FactProductivity.HasValue)
                directionsSheet.Cell(row, 9).Value = direction.FactProductivity.Value;
            if (direction.NormProductivity.HasValue)
                directionsSheet.Cell(row, 10).Value = direction.NormProductivity.Value;
            if (direction.ZScore.HasValue)
                directionsSheet.Cell(row, 11).Value = direction.ZScore.Value;
            directionsSheet.Cell(row, 12).Value = direction.Interpretation?.ToRussian() ?? string.Empty;
            if (row % 2 == 1)
                directionsSheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            row++;
        }

        directionsSheet.Columns().AdjustToContents();
        directionsSheet.SheetView.FreezeRows(2);

        var volumesSheet = workbook.Worksheets.Add("ФО и оборудование");
        volumesSheet.Cell(1, 1).Value = "Физ. объёмы и оборудование по направлениям";
        volumesSheet.Cell(1, 1).Style.Font.Bold = true;
        volumesSheet.Range(1, 1, 1, 8).Merge();

        var volHeaders = new[]
        {
            "Направление", "Тип", "Код", "Номенклатура", "Ед.", "Кол-во", "Коэфф.", "Итого",
        };
        for (var c = 0; c < volHeaders.Length; c++)
        {
            volumesSheet.Cell(2, c + 1).Value = volHeaders[c];
            volumesSheet.Cell(2, c + 1).Style.Font.Bold = true;
            volumesSheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            volumesSheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        row = 3;
        foreach (var direction in report.Directions.OrderBy(d => d.Code))
        {
            var dirLabel = string.IsNullOrWhiteSpace(direction.Code)
                ? direction.Name
                : $"{direction.Code} — {direction.Name}";
            foreach (var pv in direction.PhysicalVolumes)
            {
                WriteVolumeRow(volumesSheet, row++, dirLabel, "ФО", pv);
            }

            foreach (var eq in direction.EquipmentVolumes)
            {
                WriteVolumeRow(volumesSheet, row++, dirLabel, "Оборудование", eq);
            }
        }

        volumesSheet.Columns().AdjustToContents();
        volumesSheet.SheetView.FreezeRows(2);

        var laborSheet = workbook.Worksheets.Add("Трудоёмкость");
        laborSheet.Cell(1, 1).Value = "Виды работ (трудоёмкость проекта)";
        laborSheet.Cell(1, 1).Style.Font.Bold = true;
        laborSheet.Range(1, 1, 1, 4).Merge();

        var laborHeaders = new[] { "Шифр документации", "Марка", "Направление IIC", "Плановые часы" };
        for (var c = 0; c < laborHeaders.Length; c++)
        {
            laborSheet.Cell(2, c + 1).Value = laborHeaders[c];
            laborSheet.Cell(2, c + 1).Style.Font.Bold = true;
            laborSheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            laborSheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        row = 3;
        decimal laborTotal = 0m;
        foreach (var li in report.LaborIntensities)
        {
            laborSheet.Cell(row, 1).Value = li.DocumentationCode;
            laborSheet.Cell(row, 2).Value = li.Brand ?? string.Empty;
            laborSheet.Cell(row, 3).Value = li.WorkDirectionName ?? string.Empty;
            laborSheet.Cell(row, 4).Value = li.PlannedHours;
            laborTotal += li.PlannedHours;
            row++;
        }

        laborSheet.Cell(row, 1).Value = "ИТОГО";
        laborSheet.Cell(row, 4).Value = laborTotal;
        laborSheet.Row(row).Style.Font.Bold = true;
        laborSheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
        laborSheet.Columns().AdjustToContents();
        laborSheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteVolumeRow(
        IXLWorksheet sheet,
        int row,
        string directionLabel,
        string type,
        ProjectVersionPhysicalVolumeDto item)
    {
        sheet.Cell(row, 1).Value = directionLabel;
        sheet.Cell(row, 2).Value = type;
        sheet.Cell(row, 3).Value = item.Code;
        sheet.Cell(row, 4).Value = item.Name;
        sheet.Cell(row, 5).Value = item.UnitName ?? string.Empty;
        sheet.Cell(row, 6).Value = item.Quantity;
        sheet.Cell(row, 7).Value = item.Coefficient;
        sheet.Cell(row, 8).Value = item.Total;
        if (row % 2 == 1)
            sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
    }

    public Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _visualReports.ExportAnalysisPdfAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportForecastExcelAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _forecastService.GenerateForecastAsync(projectId, versionId);

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
