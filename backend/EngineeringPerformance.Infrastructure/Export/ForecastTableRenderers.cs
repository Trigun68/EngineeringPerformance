using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Один документ таблиц → ClosedXML и QuestPDF с общей темой.</summary>
internal static class ForecastTableRenderers
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static byte[] ToExcel(ForecastTableDocument document)
    {
        using var culture = new TemporaryCulture(Ru);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(document.SheetName);

        var headerLines = document.HeaderLines ?? [];
        WriteBanner(sheet, document.Title, headerLines);

        var excelRow = 2 + headerLines.Count + 1;
        var maxColumns = 1;
        foreach (var section in document.Sections)
        {
            if (!string.IsNullOrWhiteSpace(section.Title))
            {
                sheet.Cell(excelRow, 1).Value = section.Title;
                sheet.Cell(excelRow, 1).Style.Font.Bold = true;
                sheet.Cell(excelRow, 1).Style.Font.FontSize = 11;
                excelRow++;
            }

            WriteHeader(sheet, excelRow, section.Columns);
            excelRow++;
            foreach (var row in section.Rows)
            {
                WriteDataRow(sheet, excelRow, section.Columns, row);
                excelRow++;
            }

            maxColumns = Math.Max(maxColumns, section.Columns.Count);
            excelRow++;
        }

        var bannerLastRow = 1 + headerLines.Count;
        var mergeTo = Math.Max(1, maxColumns);
        for (var r = 1; r <= bannerLastRow; r++)
            sheet.Range(r, 1, r, mergeTo).Merge();
        sheet.SheetView.FreezeRows(bannerLastRow);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ToPdf(ForecastTableDocument document)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var pageSize = document.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4;
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(ForecastTableTheme.TableFontSize).FontColor(Colors.Black));
                page.Header().Element(c => RenderPdfBanner(c, document));
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    foreach (var section in document.Sections)
                    {
                        col.Item().Element(c => RenderSection(c, section));
                    }
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
        return pdf.GeneratePdf();
    }

    private static void WriteBanner(IXLWorksheet sheet, string title, IReadOnlyList<string> headerLines)
    {
        var bannerFill = XLColor.FromHtml(ForecastTableTheme.HeaderFillHex);
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = ForecastTableTheme.TitleFontSize;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = bannerFill;
        sheet.Row(1).Height = 22;

        for (var i = 0; i < headerLines.Count; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = headerLines[i];
            sheet.Cell(row, 1).Style.Font.FontSize = 10;
            sheet.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            sheet.Cell(row, 1).Style.Fill.BackgroundColor = bannerFill;
            sheet.Row(row).Height = 16;
        }
    }

    private static void RenderPdfBanner(IContainer container, ForecastTableDocument document)
    {
        var headerLines = document.HeaderLines ?? [];
        container.Background(Color.FromHex(ForecastTableTheme.HeaderFillHex)).Padding(8).Column(col =>
        {
            col.Item().Text(document.Title)
                .SemiBold()
                .FontSize(ForecastTableTheme.TitleFontSize)
                .FontColor(Colors.White);
            foreach (var line in headerLines)
            {
                col.Item().Text(line).FontSize(9).FontColor(Colors.White);
            }
        });
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, IReadOnlyList<ForecastTableColumn> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ForecastTableTheme.HeaderFillHex);
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        sheet.Row(row).Height = ForecastTableTheme.RowHeightExcel;
    }

    private static void WriteDataRow(
        IXLWorksheet sheet,
        int excelRow,
        IReadOnlyList<ForecastTableColumn> columns,
        ForecastTableRow row)
    {
        for (var i = 0; i < columns.Count && i < row.Cells.Count; i++)
        {
            var model = row.Cells[i];
            var cell = sheet.Cell(excelRow, i + 1);
            if (model.Number.HasValue)
            {
                cell.Value = model.Number.Value;
                cell.Style.NumberFormat.Format = columns[i].Numeric ? "#,##0.####" : "General";
            }
            else
            {
                cell.Value = model.Text;
            }

            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (columns[i].Numeric)
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            if (model.Bold || row.IsTotal)
                cell.Style.Font.Bold = true;

            ApplyFill(cell, model.Tone, row.IsTotal, excelRow);
        }

        sheet.Row(excelRow).Height = ForecastTableTheme.RowHeightExcel;
    }

    private static void ApplyFill(IXLCell cell, ForecastHourTone tone, bool isTotal, int excelRow)
    {
        if (tone == ForecastHourTone.Optimistic)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ForecastTableTheme.OptimisticFillHex);
            cell.Style.Font.FontColor = XLColor.White;
            return;
        }

        if (tone == ForecastHourTone.Pessimistic)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ForecastTableTheme.PessimisticFillHex);
            cell.Style.Font.FontColor = XLColor.White;
            return;
        }

        if (isTotal)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ForecastTableTheme.TotalFillHex);
            cell.Style.Font.FontColor = XLColor.White;
            return;
        }

        if (excelRow % 2 == 0)
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ForecastTableTheme.StripeFillHex);
    }

    private static void RenderSection(IContainer container, ForecastTableSection section)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(section.Title))
                col.Item().Text(section.Title).SemiBold().FontSize(10);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var column in section.Columns)
                        columns.RelativeColumn(column.RelativeWidth);
                });

                table.Header(header =>
                {
                    foreach (var column in section.Columns)
                    {
                        header.Cell()
                            .Background(Color.FromHex(ForecastTableTheme.HeaderFillHex))
                            .Padding(4)
                            .AlignMiddle()
                            .Text(column.Header)
                            .FontColor(Colors.White)
                            .SemiBold();
                    }
                });

                foreach (var row in section.Rows)
                {
                    for (var i = 0; i < section.Columns.Count; i++)
                    {
                        var column = section.Columns[i];
                        var model = i < row.Cells.Count
                            ? row.Cells[i]
                            : new ForecastTableCell(string.Empty);
                        var bg = CellBackground(model.Tone, row.IsTotal);
                        var fg = CellForeground(model.Tone, row.IsTotal);
                        var cell = table.Cell().Background(bg).Padding(4).AlignMiddle();
                        if (column.Numeric)
                            cell = cell.AlignRight();
                        var text = cell.Text(model.Text).FontColor(fg);
                        if (model.Bold || row.IsTotal)
                            text.SemiBold();
                    }
                }
            });
        });
    }

    private static Color CellBackground(ForecastHourTone tone, bool isTotal)
    {
        if (tone == ForecastHourTone.Optimistic)
            return Color.FromHex(ForecastTableTheme.OptimisticFillHex);
        if (tone == ForecastHourTone.Pessimistic)
            return Color.FromHex(ForecastTableTheme.PessimisticFillHex);
        if (isTotal)
            return Color.FromHex(ForecastTableTheme.TotalFillHex);
        return Colors.White;
    }

    private static Color CellForeground(ForecastHourTone tone, bool isTotal)
    {
        if (tone is ForecastHourTone.Optimistic or ForecastHourTone.Pessimistic || isTotal)
            return Colors.White;
        return Colors.Black;
    }
}
