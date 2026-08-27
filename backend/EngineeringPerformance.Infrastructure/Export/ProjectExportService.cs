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
    private readonly IProjectCompareService _compare;

    public ProjectExportService(
        IProjectsPageDataFetcher dataFetcher,
        IForecastDetailSummaryTableContext summaryTableContext,
        IProjectCompareService compare)
    {
        _dataFetcher = dataFetcher;
        _summaryTableContext = summaryTableContext;
        _compare = compare;
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
                        "Направление", "План. объём", "Опт.", "База ч", "Пессим.",
                    }, data.Forecast.Summary.Select(r => new[]
                    {
                        r.DisplayName,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.OptimisticHours?.ToString("N2") ?? "—",
                        r.BaseHours?.ToString("N2") ?? "—",
                        r.PessimisticHours?.ToString("N2") ?? "—",
                    })));

                    col.Item().Element(c => RenderTableSection(c, "Факт — итого", new[]
                    {
                        "Направление", "ФО", "Часы", "План.Труд.Оптим.", "План.Труд.Баз.", "План.Труд.Песс.",
                        "Z", "Интерпретация",
                    }, data.Fact.Performance.Select(r => new[]
                    {
                        r.WorkDirectionName,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.FactLaborHours?.ToString("N2") ?? "—",
                        r.OptimisticHours?.ToString("N2") ?? "—",
                        r.BaseHours?.ToString("N2") ?? "—",
                        r.PessimisticHours?.ToString("N2") ?? "—",
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

    public async Task<byte[]> ExportForecastSummaryExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var (data, title, headerLines) = await LoadForecastTableAsync(
            projectId, versionId, "Прогноз трудозатрат", cancellationToken);
        return ForecastTableRenderers.ToExcel(
            WithBanner(ForecastTableDocumentBuilder.BuildSummary(data.Forecast.Summary, title), headerLines));
    }

    public async Task<byte[]> ExportForecastFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var (data, title, headerLines) = await LoadForecastTableAsync(
            projectId, versionId, "Прогноз трудозатрат по объектам", cancellationToken);
        return ForecastTableRenderers.ToExcel(
            WithBanner(ForecastTableDocumentBuilder.BuildFacility(data.Forecast.Facility, title), headerLines));
    }

    public async Task<byte[]> ExportForecastDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var (data, title, headerLines) = await LoadForecastTableAsync(
            projectId, versionId, "Детализация прогноза", cancellationToken);
        return ForecastTableRenderers.ToExcel(
            WithBanner(ForecastTableDocumentBuilder.BuildDetail(data.Forecast.Detail, title), headerLines));
    }

    private static ForecastTableDocument WithBanner(ForecastTableDocument document, IReadOnlyList<string> headerLines) =>
        document with { HeaderLines = headerLines };

    private async Task<(ProjectsPageDataDto Data, string Title, IReadOnlyList<string> HeaderLines)> LoadForecastTableAsync(
        int projectId,
        int versionId,
        string caption,
        CancellationToken cancellationToken)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, projectName, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);
        return (data, $"{caption}: {projectCode} ({versionName})", TabularReportBanner.Lines(projectCode, projectName, versionName));
    }

    public async Task<byte[]> ExportForecastDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, directions) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

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

    public async Task<byte[]> ExportCompareDetailExcelAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default)
    {
        var compare = await _compare.CompareAsync(projectId, versionAId, versionBId, cancellationToken);
        return BuildCompareDetailWorkbook(
            CompareDetailSummaryTableBuilder.Build(compare.Detail),
            $"Детализация сравнения версий {versionAId} / {versionBId}");
    }

    public async Task<byte[]> ExportCompareDetailSummaryTableAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CancellationToken cancellationToken = default)
    {
        var compare = await _compare.CompareAsync(projectId, versionAId, versionBId, cancellationToken);
        return BuildCompareDetailWorkbook(
            CompareDetailSummaryTableBuilder.Build(compare.Detail),
            $"Сводная детализация сравнения версий {versionAId} / {versionBId}");
    }

    public async Task<byte[]> ExportFactSummaryExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default) =>
        await RenderFactSummaryAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportFactFacilityExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default) =>
        await RenderFactFacilityAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportFactDetailExcelAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        // PDF для детализации факта отключён: таблица на десятки тысяч строк даёт непригодный файл (13k+ страниц).
        var (data, title, headerLines) = await LoadForecastTableAsync(projectId, versionId, "Детализация оценки факта", cancellationToken);
        var document = WithBanner(FactTableDocumentBuilder.BuildDetail(data.Fact.Detail, title), headerLines);
        return ForecastTableRenderers.ToExcel(document);
    }

    private async Task<byte[]> RenderFactSummaryAsync(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var (data, title, headerLines) = await LoadForecastTableAsync(projectId, versionId, "Оценка факта", cancellationToken);
        var document = WithBanner(FactTableDocumentBuilder.BuildSummary(data.Fact.Performance, title), headerLines);
        return ForecastTableRenderers.ToExcel(document);
    }

    private async Task<byte[]> RenderFactFacilityAsync(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var (data, title, headerLines) = await LoadForecastTableAsync(projectId, versionId, "Оценка факта по объектам", cancellationToken);
        var document = WithBanner(FactTableDocumentBuilder.BuildFacility(data.Fact.Facility, title), headerLines);
        return ForecastTableRenderers.ToExcel(document);
    }

    public async Task<byte[]> ExportFactDetailSummaryTableAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, directions) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);

        var inputRows = data.Fact.Detail
            .Where(r => string.Equals(r.IsAssessment, "Да", StringComparison.Ordinal))
            .Select(r => new DetailFactSummaryInputRow(
                r.FacilityCode,
                r.FacilityName,
                r.ClassifierId,
                r.Quantity,
                r.Coefficient,
                r.PessimisticHours,
                r.BaseHours,
                r.OptimisticHours,
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
        var ws = workbook.Worksheets.Add("Сводная");

        const int colsPerBlock = 6;
        const int laborTotalSpan = 4;
        var colCount = Math.Max(1, 1 + table.Columns.Count * colsPerBlock + laborTotalSpan);
        ws.Cell(1, 1).Value = $"Сводная таблица факта: {table.ProjectCode} ({table.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
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
        ws.Cell(2, laborTotalCol).Value = "Итого";
        ws.Range(2, laborTotalCol, 2, laborTotalCol + laborTotalSpan - 1).Merge();
        var laborHeader = ws.Range(2, laborTotalCol, 2, laborTotalCol + laborTotalSpan - 1);
        laborHeader.Style.Font.Bold = true;
        laborHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
        laborHeader.Style.Font.FontColor = XLColor.White;
        laborHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        const string headerFill = "#34495E";
        ws.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(headerFill);
        blockCol = 2;
        foreach (var _ in table.Columns)
        {
            var labels = new[]
            {
                "ФО",
                "План.Труд.Оптим.",
                "План.Труд.Баз.",
                "План.Труд.Песс.",
                "Факт Тр.",
                "Z",
            };
            for (var i = 0; i < labels.Length; i++)
            {
                var cell = ws.Cell(3, blockCol + i);
                cell.Value = labels[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(headerFill);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            blockCol += colsPerBlock;
        }

        var totalLabels = new[] { "Оптим.", "Баз.", "Песс.", "Факт" };
        for (var i = 0; i < totalLabels.Length; i++)
        {
            var cell = ws.Cell(3, laborTotalCol + i);
            cell.Value = totalLabels[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(headerFill);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var rowIdx = 4;
        var columnCount = table.Columns.Count;
        var totalVolumes = new decimal[columnCount];
        var totalPes = new decimal[columnCount];
        var totalBase = new decimal[columnCount];
        var totalOpt = new decimal[columnCount];
        var totalFact = new decimal[columnCount];
        decimal grandFactLabor = 0m, grandOptLabor = 0m, grandBaseLabor = 0m, grandPesLabor = 0m;

        foreach (var dataRow in table.Rows)
        {
            ws.Cell(rowIdx, 1).Value = dataRow.RowLabel;
            var cellCol = 2;
            for (var i = 0; i < dataRow.Cells.Count; i++)
            {
                var cell = dataRow.Cells[i];
                totalVolumes[i] += cell.PhysicalVolume;
                totalPes[i] += cell.PessimisticHours;
                totalBase[i] += cell.BaseHours;
                totalOpt[i] += cell.OptimisticHours;
                totalFact[i] += cell.FactHours;

                WriteSummaryNumber(ws, rowIdx, cellCol, cell.PhysicalVolume);
                WriteSummaryNumber(ws, rowIdx, cellCol + 1, cell.OptimisticHours);
                PaintScenario(ws, rowIdx, cellCol + 1, "#e3f2fd");
                WriteSummaryNumber(ws, rowIdx, cellCol + 2, cell.BaseHours);
                PaintScenario(ws, rowIdx, cellCol + 2, "#e8f5e9");
                WriteSummaryNumber(ws, rowIdx, cellCol + 3, cell.PessimisticHours);
                PaintScenario(ws, rowIdx, cellCol + 3, "#ffebee");
                WriteSummaryNumber(ws, rowIdx, cellCol + 4, cell.FactHours);
                if (cell.ZScore.HasValue)
                {
                    ws.Cell(rowIdx, cellCol + 5).Value = cell.ZScore.Value;
                    ws.Cell(rowIdx, cellCol + 5).Style.NumberFormat.Format = "0.00";
                    ws.Cell(rowIdx, cellCol + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                cellCol += colsPerBlock;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, dataRow.OptimisticLaborHoursTotal);
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 1, dataRow.BaseLaborHoursTotal);
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 2, dataRow.PessimisticLaborHoursTotal);
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 3, dataRow.FactLaborHoursTotal);
            grandOptLabor += dataRow.OptimisticLaborHoursTotal;
            grandBaseLabor += dataRow.BaseLaborHoursTotal;
            grandPesLabor += dataRow.PessimisticLaborHoursTotal;
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
                WriteSummaryNumber(ws, rowIdx, totalCol + 1, Math.Round(totalOpt[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 2, Math.Round(totalBase[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 3, Math.Round(totalPes[i], 0, MidpointRounding.AwayFromZero));
                WriteSummaryNumber(ws, rowIdx, totalCol + 4, Math.Round(totalFact[i], 0, MidpointRounding.AwayFromZero));
                totalCol += colsPerBlock;
            }

            WriteSummaryNumber(ws, rowIdx, laborTotalCol, Math.Round(grandOptLabor, 0, MidpointRounding.AwayFromZero));
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 1, Math.Round(grandBaseLabor, 0, MidpointRounding.AwayFromZero));
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 2, Math.Round(grandPesLabor, 0, MidpointRounding.AwayFromZero));
            WriteSummaryNumber(ws, rowIdx, laborTotalCol + 3, Math.Round(grandFactLabor, 0, MidpointRounding.AwayFromZero));
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

    private static void PaintScenario(IXLWorksheet ws, int row, int col, string hex)
    {
        if (ws.Cell(row, col).IsEmpty())
            return;
        ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml(hex);
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
            "Факт ч", "План.Труд.Оптим.", "План.Труд.Баз.", "План.Труд.Песс.",
            "Факт произв.", "Z", "Интерпретация",
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
                // Boxed nullable без значения → null; со значением → T (не T?).
                cell.Value = value switch
                {
                    null => string.Empty,
                    decimal d => d,
                    double dbl => dbl,
                    float flt => flt,
                    int i => i,
                    long l => l,
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
            Set(row.FactLaborHours);
            Set(row.OptimisticHours);
            Set(row.BaseHours);
            Set(row.PessimisticHours);
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

    private static byte[] BuildCompareDetailWorkbook(
        IReadOnlyList<CompareDetailSummaryTableRowDto> rows,
        string title)
    {
        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Сравнение детально");
        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта", "Группа ФО IIC", "Код позиции", "Наименование позиции",
            "ID работы", "Наименование работы", "ФО A", "ФО B",
            "План.Труд. (Опт. v. А)", "План.Труд. (Баз. v. А)", "План.Труд. (Песс. v. А)",
            "План.Труд. (Опт. v. Б)", "План.Труд. (Баз. v. Б)", "План.Труд. (Песс. v. Б)",
            "Факт. труд A", "Факт. труд B", "План. произв. A", "План. произв. B",
            "Факт. произв. A", "Факт. произв. B", "Z A", "Z B", "Наличие",
        };

        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 13;
        sheet.Range(1, 1, 1, headers.Length).Merge();

        for (var column = 0; column < headers.Length; column++)
        {
            var header = sheet.Cell(2, column + 1);
            header.Value = headers[column];
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Alignment.WrapText = true;
        }

        var rowNumber = 3;
        foreach (var row in rows)
        {
            sheet.Cell(rowNumber, 1).Value = row.FacilityCode;
            sheet.Cell(rowNumber, 2).Value = row.FacilityName;
            sheet.Cell(rowNumber, 3).Value = row.IicGroupName;
            sheet.Cell(rowNumber, 4).Value = row.PvCode;
            sheet.Cell(rowNumber, 5).Value = row.PvName;
            sheet.Cell(rowNumber, 6).Value = row.WorkId;
            sheet.Cell(rowNumber, 7).Value = row.WorkName;
            WriteNullableNumber(sheet, rowNumber, 8, row.PhysicalVolumeA);
            WriteNullableNumber(sheet, rowNumber, 9, row.PhysicalVolumeB);
            WriteNullableNumber(sheet, rowNumber, 10, row.PlannedLaborHoursOptimisticA);
            WriteNullableNumber(sheet, rowNumber, 11, row.PlannedLaborHoursA);
            WriteNullableNumber(sheet, rowNumber, 12, row.PlannedLaborHoursPessimisticA);
            WriteNullableNumber(sheet, rowNumber, 13, row.PlannedLaborHoursOptimisticB);
            WriteNullableNumber(sheet, rowNumber, 14, row.PlannedLaborHoursB);
            WriteNullableNumber(sheet, rowNumber, 15, row.PlannedLaborHoursPessimisticB);
            WriteNullableNumber(sheet, rowNumber, 16, row.FactLaborHoursA);
            WriteNullableNumber(sheet, rowNumber, 17, row.FactLaborHoursB);
            WriteNullableNumber(sheet, rowNumber, 18, row.PlannedProductivityA);
            WriteNullableNumber(sheet, rowNumber, 19, row.PlannedProductivityB);
            WriteNullableNumber(sheet, rowNumber, 20, row.FactProductivityA);
            WriteNullableNumber(sheet, rowNumber, 21, row.FactProductivityB);
            WriteNullableNumber(sheet, rowNumber, 22, row.ZScoreA);
            WriteNullableNumber(sheet, rowNumber, 23, row.ZScoreB);
            sheet.Cell(rowNumber, 24).Value = row.Presence;

            if (rowNumber % 2 == 1)
                sheet.Row(rowNumber).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            rowNumber++;
        }

        sheet.Range(2, 1, Math.Max(2, rowNumber - 1), headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(2, 1, Math.Max(2, rowNumber - 1), headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        sheet.SheetView.FreezeRows(2);
        sheet.Columns(1, 7).AdjustToContents(1, Math.Max(2, rowNumber - 1));
        for (var column = 8; column <= 23; column++)
            sheet.Column(column).Width = 15;
        sheet.Column(24).Width = 14;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteNullableNumber(IXLWorksheet sheet, int row, int column, decimal? value)
    {
        if (!value.HasValue)
            return;

        var cell = sheet.Cell(row, column);
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "#,##0.####";
    }

    private static void WriteForecastSheet(XLWorkbook workbook, ProjectsPageDataDto data)
    {
        var sheet = workbook.Worksheets.Add("Forecast");
        sheet.Cell(1, 1).Value = "Направление";
        sheet.Cell(1, 2).Value = "План. объём";
        sheet.Cell(1, 3).Value = "Опт.";
        sheet.Cell(1, 4).Value = "База ч";
        sheet.Cell(1, 5).Value = "Пессим.";
        var row = 2;
        foreach (var item in data.Forecast.Summary)
        {
            sheet.Cell(row, 1).Value = item.DisplayName;
            sheet.Cell(row, 2).Value = item.FactPhysicalVolume;
            sheet.Cell(row, 3).Value = item.OptimisticHours;
            sheet.Cell(row, 4).Value = item.BaseHours;
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
        sheet.Cell(1, 4).Value = "План.Труд.Оптим.";
        sheet.Cell(1, 5).Value = "План.Труд.Баз.";
        sheet.Cell(1, 6).Value = "План.Труд.Песс.";
        sheet.Cell(1, 7).Value = "Z";
        var row = 2;
        foreach (var item in data.Fact.Performance)
        {
            sheet.Cell(row, 1).Value = item.WorkDirectionName;
            sheet.Cell(row, 2).Value = item.FactPhysicalVolume;
            sheet.Cell(row, 3).Value = item.FactLaborHours;
            sheet.Cell(row, 4).Value = item.OptimisticHours;
            sheet.Cell(row, 5).Value = item.BaseHours;
            sheet.Cell(row, 6).Value = item.PessimisticHours;
            sheet.Cell(row, 7).Value = item.ZScore;
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
            "Оптимистический", "Базовый", "Пессимистический",
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
                // Boxed nullable без значения → null; со значением → T (не T?).
                cell.Value = value switch
                {
                    null => string.Empty,
                    decimal d => d,
                    double dbl => dbl,
                    float flt => flt,
                    int i => i,
                    long l => l,
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
                Set(row.OptimisticHours);
                Set(row.BaseHours);
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
