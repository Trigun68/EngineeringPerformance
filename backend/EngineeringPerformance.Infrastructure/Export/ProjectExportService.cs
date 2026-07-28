using System.Globalization;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Экспорт версии проекта: данные из <see cref="IProjectsPageDataFetcher"/> (паритет с UI).
/// QuestPDF / ClosedXML — строго using для освобождения ресурсов.
/// </summary>
public sealed class ProjectExportService : IProjectExportService
{
    private readonly IProjectsPageDataFetcher _dataFetcher;
    private readonly IForecastDetailSummaryTableContext _summaryTableContext;

    public ProjectExportService(
        IProjectsPageDataFetcher dataFetcher,
        IForecastDetailSummaryTableContext summaryTableContext)
    {
        _dataFetcher = dataFetcher;
        _summaryTableContext = summaryTableContext;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportToPdfAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Отчёт по проекту").SemiBold().FontSize(16);
                    col.Item().Text($"ProjectId: {projectId}, VersionId: {versionId}");
                    col.Item().Text($"Сформирован: {DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)):yyyy-MM-dd HH:mm} MSK").FontSize(8);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(c => RenderTableSection(c, "Прогноз — итого", new[]
                    {
                        "Направление", "План. объём", "База ч", "Опт.", "Пессим.",
                    }, data.Forecast.Summary.Select(r => new[]
                    {
                        r.DisplayName,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.BaseHours?.ToString("N2") ?? "—",
                        r.OptimisticHours?.ToString("N2") ?? "—",
                        r.PessimisticHours?.ToString("N2") ?? "—",
                    })));

                    col.Item().Element(c => RenderTableSection(c, "Факт — итого", new[]
                    {
                        "Направление", "ФО", "Часы", "Z", "Интерпретация",
                    }, data.Fact.Performance.Select(r => new[]
                    {
                        r.WorkDirectionName,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.FactLaborHours?.ToString("N2") ?? "—",
                        r.ZScore?.ToString("N4") ?? "—",
                        r.Interpretation?.ToRussian() ?? "—",
                    })));

                    var equipmentRows = data.Fact.Detail.Where(d => d.IsEquipment).ToList();
                    col.Item().Element(c => RenderTableSection(c, "Оборудование", new[]
                    {
                        "Объект", "Наименование", "Кол-во", "Часы",
                    }, equipmentRows.Select(r => new[]
                    {
                        r.FacilityName,
                        r.CatalogName,
                        r.Quantity.ToString("N2"),
                        r.FactLaborHours?.ToString("N2") ?? r.BaseHours?.ToString("N2") ?? "—",
                    })));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Engineering Performance · ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportToExcelAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();

        WriteForecastSheet(workbook, data);
        WriteFactSheet(workbook, data);

        var ppvRows = data.Fact.Detail.Where(d => !d.IsEquipment).ToList();
        WriteDetailSheet(workbook.Worksheets.Add("PPV"), ppvRows, includeForecastHours: false);

        var equipmentRows = data.Fact.Detail.Where(d => d.IsEquipment).ToList();
        WriteDetailSheet(workbook.Worksheets.Add("Equipment"), equipmentRows, includeForecastHours: true);

        var liSheet = workbook.Worksheets.Add("LI");
        liSheet.Cell(1, 1).Value = "Направление";
        liSheet.Cell(1, 2).Value = "ФО";
        liSheet.Cell(1, 3).Value = "Часы";
        liSheet.Cell(1, 4).Value = "Z";
        liSheet.Cell(1, 5).Value = "Интерпретация";
        var liRow = 2;
        foreach (var row in data.Fact.Performance)
        {
            liSheet.Cell(liRow, 1).Value = row.WorkDirectionName;
            liSheet.Cell(liRow, 2).Value = row.FactPhysicalVolume;
            liSheet.Cell(liRow, 3).Value = row.FactLaborHours;
            liSheet.Cell(liRow, 4).Value = row.ZScore;
            liSheet.Cell(liRow, 5).Value = row.Interpretation?.ToRussian() ?? string.Empty;
            liRow++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportForecastFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("По объектам");
        sheet.Cell(1, 1).Value = $"Прогноз трудозатрат по объектам: {projectCode} ({versionName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Шифр объекта", "Наименование объекта", "Базовый", "Оптимистический", "Пессимистический" };
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Cell(2, c + 1).Style.Font.Bold = true;
            sheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
        foreach (var item in data.Forecast.Facility)
        {
            var baseH = item.BaseHours ?? item.LaborHours;
            sheet.Cell(row, 1).Value = item.FacilityCode;
            sheet.Cell(row, 2).Value = item.FacilityName;
            if (baseH.HasValue) sheet.Cell(row, 3).Value = Math.Round(baseH.Value, 0);
            if (item.OptimisticHours.HasValue) sheet.Cell(row, 4).Value = Math.Round(item.OptimisticHours.Value, 0);
            if (item.PessimisticHours.HasValue) sheet.Cell(row, 5).Value = Math.Round(item.PessimisticHours.Value, 0);
            if (row % 2 == 1)
                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            totalBase += baseH ?? 0m;
            totalOpt += item.OptimisticHours ?? 0m;
            totalPes += item.PessimisticHours ?? 0m;
            row++;
        }

        sheet.Cell(row, 1).Value = "ИТОГО";
        sheet.Cell(row, 3).Value = Math.Round(totalBase, 0);
        sheet.Cell(row, 4).Value = Math.Round(totalOpt, 0);
        sheet.Cell(row, 5).Value = Math.Round(totalPes, 0);
        sheet.Row(row).Style.Font.Bold = true;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        sheet.Row(row).Style.Font.FontColor = XLColor.White;

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 55;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 14;
        sheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportForecastFacilityPdfAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Column(col =>
                {
                    col.Item().Text($"Прогноз по объектам: {projectCode} ({versionName})").SemiBold().FontSize(13);
                });
                page.Content().Element(c => RenderFacilityForecastTable(c, data.Forecast.Facility));
            });
        });
        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportForecastDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Детально");
        sheet.Cell(1, 1).Value = $"Детализация прогноза: {projectCode} ({versionName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, 22).Merge();

        WriteDetailSheet(sheet, data.Forecast.Detail, includeForecastHours: true, headerRow: 2);
        sheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportForecastDetailPdfAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8));
                page.Header().Column(col =>
                {
                    col.Item().Text($"Детализация прогноза: {projectCode} ({versionName})").SemiBold().FontSize(13);
                });
                page.Content().Element(c => RenderDetailForecastTable(c, data.Forecast.Detail));
            });
        });
        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportForecastDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, directions) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        var assessed = data.Forecast.Detail
            .Where(r => string.Equals(r.IsAssessment, "Да", StringComparison.Ordinal))
            .ToList();

        DetailForecastSummaryTableDto BuildForScenario(Func<ProjectsPageDetailRowDto, decimal?> hoursSelector) =>
            DetailForecastSummaryTableBuilder.Build(
                assessed
                    .Select(r => new DetailForecastSummaryInputRow(
                        r.FacilityCode,
                        r.FacilityName,
                        r.ClassifierId,
                        r.Quantity,
                        r.Coefficient,
                        hoursSelector(r)))
                    .ToList(),
                directions,
                projectCode,
                versionName);

        var baseTable = BuildForScenario(r => r.BaseHours);
        var optimisticTable = BuildForScenario(r => r.OptimisticHours);
        var pessimisticTable = BuildForScenario(r => r.PessimisticHours);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        AddDetailSummarySheet(workbook, "Базовый", baseTable, isForecast: true);
        AddDetailSummarySheet(workbook, "Оптимистичный", optimisticTable, isForecast: true);
        AddDetailSummarySheet(workbook, "Пессимистичный", pessimisticTable, isForecast: true);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportFactFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("По объектам");
        sheet.Cell(1, 1).Value = $"Оценка факта по объектам: {projectCode} ({versionName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, 5).Merge();

        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта", "Факт.трудоёмк., ч", "Z-оценка", "Интерпретация",
        };
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(2, c + 1).Value = headers[c];
            sheet.Cell(2, c + 1).Style.Font.Bold = true;
            sheet.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(2, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        decimal totalHours = 0m;
        foreach (var item in data.Fact.Facility)
        {
            sheet.Cell(row, 1).Value = item.FacilityCode;
            sheet.Cell(row, 2).Value = item.FacilityName;
            if (item.LaborHours.HasValue)
            {
                sheet.Cell(row, 3).Value = Math.Round(item.LaborHours.Value, 2);
                totalHours += item.LaborHours.Value;
            }

            if (item.ZScore.HasValue)
                sheet.Cell(row, 4).Value = item.ZScore.Value;
            sheet.Cell(row, 5).Value = item.Interpretation ?? string.Empty;
            if (row % 2 == 1)
                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            row++;
        }

        sheet.Cell(row, 1).Value = "ИТОГО";
        sheet.Cell(row, 3).Value = Math.Round(totalHours, 2);
        sheet.Row(row).Style.Font.Bold = true;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        sheet.Row(row).Style.Font.FontColor = XLColor.White;

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 55;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 12;
        sheet.Column(5).Width = 22;
        sheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportFactDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Детально");
        sheet.Cell(1, 1).Value = $"Детализация оценки факта: {projectCode} ({versionName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, 22).Merge();

        WriteFactDetailSheet(sheet, data.Fact.Detail, headerRow: 2);
        sheet.SheetView.FreezeRows(2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportFactDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, directions) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        var inputRows = data.Fact.Detail
            .Where(r => string.Equals(r.IsAssessment, "Да", StringComparison.Ordinal))
            .Select(r => new DetailFactSummaryInputRow(
                r.FacilityCode,
                r.FacilityName,
                r.ClassifierId,
                r.Quantity,
                r.Coefficient,
                r.BaseHours,
                r.FactLaborHours,
                r.ZScore))
            .ToList();

        var table = DetailFactSummaryTableBuilder.Build(
            inputRows,
            directions,
            projectCode,
            versionName);

        using var workbook = BuildFactDetailSummaryTableWorkbook(table);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLWorkbook BuildFactDetailSummaryTableWorkbook(DetailFactSummaryTableDto table)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Базовый");

        const int colsPerBlock = 4;
        var colCount = Math.Max(1, 1 + table.Columns.Count * colsPerBlock + 1);
        ws.Cell(1, 1).Value = $"Сводная таблица факта (Базовый): {table.ProjectCode} ({table.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, colCount).Merge();

        ws.Cell(2, 1).Value = "Объект";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(2, 1, 3, 1).Merge();

        var blockCol = 2;
        foreach (var column in table.Columns)
        {
            ws.Cell(2, blockCol).Value = column.BlockTitle;
            ws.Range(2, blockCol, 2, blockCol + colsPerBlock - 1).Merge();
            var blockRange = ws.Range(2, blockCol, 2, blockCol + colsPerBlock - 1);
            blockRange.Style.Font.Bold = true;
            blockRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            blockRange.Style.Font.FontColor = XLColor.White;
            blockRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            blockRange.Style.Alignment.WrapText = true;
            blockCol += colsPerBlock;
        }

        var laborTotalCol = blockCol;
        ws.Cell(2, laborTotalCol).Value = "Итого факт Тр.";
        ws.Range(2, laborTotalCol, 3, laborTotalCol).Merge();
        var laborHeader = ws.Range(2, laborTotalCol, 3, laborTotalCol);
        laborHeader.Style.Font.Bold = true;
        laborHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        laborHeader.Style.Font.FontColor = XLColor.White;
        laborHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        laborHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
        blockCol = 2;
        foreach (var _ in table.Columns)
        {
            var labels = new[] { "ФО", "План Тр.", "Факт Тр.", "Z" };
            for (var i = 0; i < labels.Length; i++)
            {
                var cell = ws.Cell(3, blockCol + i);
                cell.Value = labels[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            blockCol += colsPerBlock;
        }

        var rowIdx = 4;
        var columnCount = table.Columns.Count;
        var totalVolumes = new decimal[columnCount];
        var totalPlan = new decimal[columnCount];
        var totalFact = new decimal[columnCount];
        decimal grandFactLabor = 0m;

        foreach (var dataRow in table.Rows)
        {
            ws.Cell(rowIdx, 1).Value = dataRow.RowLabel;
            var cellCol = 2;
            for (var i = 0; i < dataRow.Cells.Count; i++)
            {
                var cell = dataRow.Cells[i];
                totalVolumes[i] += cell.PhysicalVolume;
                totalPlan[i] += cell.PlanHours;
                totalFact[i] += cell.FactHours;

                WriteSummaryNumber(ws, rowIdx, cellCol, cell.PhysicalVolume);
                WriteSummaryNumber(ws, rowIdx, cellCol + 1, cell.PlanHours);
                WriteSummaryNumber(ws, rowIdx, cellCol + 2, cell.FactHours);
                if (cell.ZScore.HasValue)
                {
                    ws.Cell(rowIdx, cellCol + 3).Value = cell.ZScore.Value;
                    ws.Cell(rowIdx, cellCol + 3).Style.NumberFormat.Format = "0.00";
                    ws.Cell(rowIdx, cellCol + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                cellCol += colsPerBlock;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, dataRow.FactLaborHoursTotal);
            grandFactLabor += dataRow.FactLaborHoursTotal;

            if (rowIdx % 2 == 0)
                ws.Range(rowIdx, 1, rowIdx, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            rowIdx++;
        }

        if (table.Rows.Count > 0)
        {
            ws.Cell(rowIdx, 1).Value = "ИТОГО";
            ws.Row(rowIdx).Style.Font.Bold = true;
            ws.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");

            var totalCol = 2;
            for (var i = 0; i < columnCount; i++)
            {
                WriteSummaryNumber(ws, rowIdx, totalCol, Math.Round(totalVolumes[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 1, Math.Round(totalPlan[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 2, Math.Round(totalFact[i], 0, MidpointRounding.AwayFromZero));
                totalCol += colsPerBlock;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, Math.Round(grandFactLabor, 0, MidpointRounding.AwayFromZero));
            rowIdx++;
        }

        if (rowIdx > 3)
        {
            ws.Range(2, 1, rowIdx - 1, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(2, 1, rowIdx - 1, colCount).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }

        ws.Column(1).Width = 36;
        for (var c = 2; c <= colCount; c++)
            ws.Column(c).Width = 12;

        ws.SheetView.FreezeRows(3);
        return workbook;
    }

    private const string SummaryIntegerNumberFormat = "#,##0";

    private static void WriteSummaryNumber(IXLWorksheet ws, int row, int col, decimal value)
    {
        if (value == 0m)
            return;
        ws.Cell(row, col).Value = value;
        ws.Cell(row, col).Style.NumberFormat.Format = SummaryIntegerNumberFormat;
        ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void WriteFactDetailSheet(
        IXLWorksheet sheet,
        IReadOnlyList<ProjectsPageDetailRowDto> rows,
        int headerRow = 1)
    {
        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта", "Тип привязки", "Комплект документов",
            "ID по каталогу", "TAG", "ID работы", "Наименование", "Комментарий",
            "Описательная часть", "Код", "Наименование ФО", "Оценка", "Коэфф.",
            "Группа ФО IIC", "Ед. изм.", "Производит.", "СО", "Кол-во",
            "План ч (база)", "Факт ч", "Факт произв.", "Z", "Интерпретация",
        };

        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(headerRow, c + 1).Value = headers[c];
            sheet.Cell(headerRow, c + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var rowIndex = headerRow + 1;
        foreach (var row in rows)
        {
            var col = 1;
            void Set(object? value)
            {
                var cell = sheet.Cell(rowIndex, col++);
                cell.Value = value switch
                {
                    decimal? d => d.HasValue ? (object)d.Value : string.Empty,
                    decimal d => d,
                    double? dbl => dbl.HasValue ? (object)dbl.Value : string.Empty,
                    double dbl => dbl,
                    float? flt => flt.HasValue ? (object)flt.Value : string.Empty,
                    float flt => flt,
                    int? i => i.HasValue ? (object)i.Value : string.Empty,
                    int i => i,
                    long? l => l.HasValue ? (object)l.Value : string.Empty,
                    long l => l,
                    null => string.Empty,
                    _ => value.ToString() ?? string.Empty,
                };
            }

            Set(row.FacilityCode);
            Set(row.FacilityName);
            Set(row.BindingType);
            Set(row.DocumentSet);
            Set(row.CatalogId);
            Set(row.Tag);
            Set(row.WorkId);
            Set(row.ItemName);
            Set(row.WorkComment);
            Set(row.EstimationBasis);
            Set(row.PvCode);
            Set(row.PvName);
            Set(row.IsAssessment);
            Set(row.Coefficient);
            Set(row.IicGroupName);
            Set(row.IicUnitName);
            Set(row.NormProductivity);
            Set(row.NormStdDev);
            Set(row.Quantity);
            Set(row.BaseHours);
            Set(row.FactLaborHours);
            Set(row.FactProductivity);
            Set(row.ZScore);
            Set(row.Interpretation);

            if (rowIndex % 2 == 0)
                sheet.Row(rowIndex).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            rowIndex++;
        }

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 28;
        for (var c = 3; c <= headers.Length; c++)
            sheet.Column(c).Width = 14;
    }

    /// <summary>
    /// Лист сводной таблицы прогноза: блоки ФО/Тр. + столбец «Итого Тр.» (без суммирования ФО).
    /// </summary>
    private static void AddDetailSummarySheet(
        XLWorkbook workbook,
        string sheetName,
        DetailForecastSummaryTableDto table,
        bool isForecast)
    {
        var ws = workbook.Worksheets.Add(sheetName);

        var hasLaborTotal = true;
        var colCount = Math.Max(1, 1 + table.Columns.Count * 2 + (hasLaborTotal ? 1 : 0));
        var titleKind = isForecast ? "прогноза" : "факта";
        ws.Cell(1, 1).Value = $"Сводная таблица {titleKind} ({sheetName}): {table.ProjectCode} ({table.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, colCount).Merge();

        ws.Cell(2, 1).Value = "Объект";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(2, 1, 3, 1).Merge();

        var blockCol = 2;
        foreach (var column in table.Columns)
        {
            ws.Cell(2, blockCol).Value = column.BlockTitle;
            ws.Range(2, blockCol, 2, blockCol + 1).Merge();
            var blockRange = ws.Range(2, blockCol, 2, blockCol + 1);
            blockRange.Style.Font.Bold = true;
            blockRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            blockRange.Style.Font.FontColor = XLColor.White;
            blockRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            blockRange.Style.Alignment.WrapText = true;
            blockCol += 2;
        }

        var laborTotalCol = blockCol;
        ws.Cell(2, laborTotalCol).Value = "Итого Тр.";
        ws.Range(2, laborTotalCol, 3, laborTotalCol).Merge();
        var laborHeader = ws.Range(2, laborTotalCol, 3, laborTotalCol);
        laborHeader.Style.Font.Bold = true;
        laborHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        laborHeader.Style.Font.FontColor = XLColor.White;
        laborHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        laborHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
        blockCol = 2;
        foreach (var _ in table.Columns)
        {
            ws.Cell(3, blockCol).Value = "ФО";
            ws.Cell(3, blockCol + 1).Value = "Тр.";
            ws.Cell(3, blockCol).Style.Font.Bold = true;
            ws.Cell(3, blockCol + 1).Style.Font.Bold = true;
            ws.Cell(3, blockCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            ws.Cell(3, blockCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            ws.Cell(3, blockCol).Style.Font.FontColor = XLColor.White;
            ws.Cell(3, blockCol + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(3, blockCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(3, blockCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            blockCol += 2;
        }

        var rowIdx = 4;
        var columnCount = table.Columns.Count;
        var totalVolumes = new decimal[columnCount];
        var totalHours = new decimal[columnCount];
        decimal grandLaborTotal = 0m;

        foreach (var dataRow in table.Rows)
        {
            ws.Cell(rowIdx, 1).Value = dataRow.RowLabel;
            var cellCol = 2;
            for (var i = 0; i < dataRow.Cells.Count; i++)
            {
                var cell = dataRow.Cells[i];
                totalVolumes[i] += cell.PhysicalVolume;
                totalHours[i] += cell.LaborHours;

                WriteSummaryNumber(ws, rowIdx, cellCol, cell.PhysicalVolume);
                WriteSummaryNumber(ws, rowIdx, cellCol + 1, cell.LaborHours);
                cellCol += 2;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, dataRow.LaborHoursTotal);
            grandLaborTotal += dataRow.LaborHoursTotal;

            if (rowIdx % 2 == 0)
                ws.Range(rowIdx, 1, rowIdx, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            rowIdx++;
        }

        if (table.Rows.Count > 0)
        {
            ws.Cell(rowIdx, 1).Value = "ИТОГО";
            ws.Row(rowIdx).Style.Font.Bold = true;
            ws.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");

            var totalCol = 2;
            for (var i = 0; i < columnCount; i++)
            {
                WriteSummaryNumber(ws, rowIdx, totalCol, Math.Round(totalVolumes[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 1, Math.Round(totalHours[i], 0, MidpointRounding.AwayFromZero));
                totalCol += 2;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, Math.Round(grandLaborTotal, 0, MidpointRounding.AwayFromZero));
            rowIdx++;
        }

        if (rowIdx > 3)
        {
            ws.Range(2, 1, rowIdx - 1, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(2, 1, rowIdx - 1, colCount).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }

        ws.Column(1).Width = 36;
        for (var c = 2; c <= colCount; c++)
            ws.Column(c).Width = 14;

        ws.SheetView.FreezeRows(3);
    }

    private static XLWorkbook BuildDetailSummaryTableWorkbook(DetailForecastSummaryTableDto table)
    {
        // Совместимость: один лист «Базовый» (тесты/старые вызовы).
        var workbook = new XLWorkbook();
        AddDetailSummarySheet(workbook, "Базовый", table, isForecast: true);
        return workbook;
    }

    private static void WriteForecastSheet(XLWorkbook workbook, ProjectsPageDataDto data)
    {
        var sheet = workbook.Worksheets.Add("Forecast");
        sheet.Cell(1, 1).Value = "Направление";
        sheet.Cell(1, 2).Value = "План. объём";
        sheet.Cell(1, 3).Value = "База ч";
        sheet.Cell(1, 4).Value = "Опт.";
        sheet.Cell(1, 5).Value = "Пессим.";
        var row = 2;
        foreach (var item in data.Forecast.Summary)
        {
            sheet.Cell(row, 1).Value = item.DisplayName;
            sheet.Cell(row, 2).Value = item.FactPhysicalVolume;
            sheet.Cell(row, 3).Value = item.BaseHours;
            sheet.Cell(row, 4).Value = item.OptimisticHours;
            sheet.Cell(row, 5).Value = item.PessimisticHours;
            row++;
        }
    }

    private static void WriteFactSheet(XLWorkbook workbook, ProjectsPageDataDto data)
    {
        var sheet = workbook.Worksheets.Add("Fact");
        sheet.Cell(1, 1).Value = "Направление";
        sheet.Cell(1, 2).Value = "ФО";
        sheet.Cell(1, 3).Value = "Часы";
        sheet.Cell(1, 4).Value = "Z";
        var row = 2;
        foreach (var item in data.Fact.Performance)
        {
            sheet.Cell(row, 1).Value = item.WorkDirectionName;
            sheet.Cell(row, 2).Value = item.FactPhysicalVolume;
            sheet.Cell(row, 3).Value = item.FactLaborHours;
            sheet.Cell(row, 4).Value = item.ZScore;
            row++;
        }
    }

    private static void WriteDetailSheet(
        IXLWorksheet sheet,
        IReadOnlyList<ProjectsPageDetailRowDto> rows,
        bool includeForecastHours,
        int headerRow = 1)
    {
        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта", "Тип привязки", "Комплект документов",
            "ID по каталогу", "TAG", "ID работы", "Наименование", "Комментарий",
            "Описательная часть", "Код", "Наименование ФО", "Оценка", "Коэфф.",
            "Группа ФО IIC", "Ед. изм.", "Производит.", "СО", "Кол-во",
            "Базовый", "Оптимистический", "Пессимистический",
        };

        if (!includeForecastHours)
            headers = headers.Take(19).Concat(new[] { "Факт часы" }).ToArray();

        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(headerRow, c + 1).Value = headers[c];
            sheet.Cell(headerRow, c + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var rowIndex = headerRow + 1;
        foreach (var row in rows)
        {
            var col = 1;
            void Set(object? value)
            {
                var cell = sheet.Cell(rowIndex, col++);
                cell.Value = value switch
                {
                    decimal? d => d.HasValue ? (object)d.Value : string.Empty,
                    decimal d => d,
                    double? dbl => dbl.HasValue ? (object)dbl.Value : string.Empty,
                    double dbl => dbl,
                    float? flt => flt.HasValue ? (object)flt.Value : string.Empty,
                    float flt => flt,
                    int? i => i.HasValue ? (object)i.Value : string.Empty,
                    int i => i,
                    long? l => l.HasValue ? (object)l.Value : string.Empty,
                    long l => l,
                    null => string.Empty,
                    _ => value.ToString() ?? string.Empty,
                };
            }

            Set(row.FacilityCode);
            Set(row.FacilityName);
            Set(row.BindingType);
            Set(row.DocumentSet);
            Set(row.CatalogId);
            Set(row.Tag);
            Set(row.WorkId);
            Set(row.ItemName);
            Set(row.WorkComment);
            Set(row.EstimationBasis);
            Set(row.PvCode);
            Set(row.PvName);
            Set(row.IsAssessment);
            Set(row.Coefficient);
            Set(row.IicGroupName);
            Set(row.IicUnitName);
            Set(row.NormProductivity);
            Set(row.NormStdDev);
            Set(row.Quantity);
            if (includeForecastHours)
            {
                Set(row.BaseHours);
                Set(row.OptimisticHours);
                Set(row.PessimisticHours);
            }
            else
            {
                Set(row.FactLaborHours);
            }

            if (rowIndex % 2 == 0)
                sheet.Row(rowIndex).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            rowIndex++;
        }

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 28;
        for (var c = 3; c <= headers.Length; c++)
            sheet.Column(c).Width = 14;
    }

    private static void RenderFacilityForecastTable(
        IContainer container,
        IReadOnlyList<ProjectsPageFacilityRowDto> rows)
    {
        container.Column(col =>
        {
            col.Item().Text("Прогноз по объектам").SemiBold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                var headers = new[] { "Шифр объекта", "Наименование объекта", "Базовый", "Оптимистический", "Пессимистический" };
                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).SemiBold();
                });

                decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
                foreach (var f in rows)
                {
                    var baseH = f.BaseHours ?? f.LaborHours;
                    totalBase += baseH ?? 0m;
                    totalOpt += f.OptimisticHours ?? 0m;
                    totalPes += f.PessimisticHours ?? 0m;
                    table.Cell().Padding(3).Text(f.FacilityCode);
                    table.Cell().Padding(3).Text(f.FacilityName);
                    table.Cell().Padding(3).AlignRight().Text(baseH?.ToString("N0") ?? "—");
                    table.Cell().Padding(3).AlignRight().Text(f.OptimisticHours?.ToString("N0") ?? "—");
                    table.Cell().Padding(3).AlignRight().Text(f.PessimisticHours?.ToString("N0") ?? "—");
                }

                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("ИТОГО").SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(string.Empty);
                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text(totalBase.ToString("N0")).SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text(totalOpt.ToString("N0")).SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text(totalPes.ToString("N0")).SemiBold();
            });
        });
    }

    private static void RenderDetailForecastTable(
        IContainer container,
        IReadOnlyList<ProjectsPageDetailRowDto> rows)
    {
        container.Column(col =>
        {
            col.Item().Text("Детализация прогноза").SemiBold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                var headers = new[]
                {
                    "Шифр объекта", "Наименование объекта", "Наименование ФО",
                    "Кол-во", "Базовый", "Оптимистический", "Пессимистический",
                };
                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(h).SemiBold();
                });

                decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
                foreach (var row in rows)
                {
                    totalBase += row.BaseHours ?? 0m;
                    totalOpt += row.OptimisticHours ?? 0m;
                    totalPes += row.PessimisticHours ?? 0m;
                    table.Cell().Padding(2).Text(row.FacilityCode);
                    table.Cell().Padding(2).Text(row.FacilityName);
                    table.Cell().Padding(2).Text(row.PvName);
                    table.Cell().Padding(2).AlignRight().Text(row.Quantity.ToString("N2"));
                    table.Cell().Padding(2).AlignRight().Text(row.BaseHours?.ToString("N0") ?? "—");
                    table.Cell().Padding(2).AlignRight().Text(row.OptimisticHours?.ToString("N0") ?? "—");
                    table.Cell().Padding(2).AlignRight().Text(row.PessimisticHours?.ToString("N0") ?? "—");
                }

                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("ИТОГО").SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text(string.Empty);
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text(string.Empty);
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text(string.Empty);
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text(totalBase.ToString("N0")).SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text(totalOpt.ToString("N0")).SemiBold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text(totalPes.ToString("N0")).SemiBold();
            });
        });
    }

    private static void RenderTableSection(
        IContainer container,
        string title,
        string[] headers,
        IEnumerable<string[]> rows)
    {
        container.Column(col =>
        {
            col.Item().Text(title).SemiBold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < headers.Length; i++)
                        columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).SemiBold();
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cell);
                }
            });
        });
    }
}

/// <summary>
/// Temporarily sets <see cref="Thread.CurrentThread.CurrentCulture"/> and restores it on dispose.
/// </summary>
internal sealed class TemporaryCulture : IDisposable
{
    private readonly CultureInfo _previous;

    public TemporaryCulture(CultureInfo culture)
    {
        _previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = culture;
    }

    public void Dispose()
    {
        Thread.CurrentThread.CurrentCulture = _previous;
    }
}
