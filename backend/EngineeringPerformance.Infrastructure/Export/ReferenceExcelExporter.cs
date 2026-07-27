using System.Globalization;
using ClosedXML.Excel;

namespace EngineeringPerformance.Infrastructure.Export;

public static class ReferenceExcelExporter
{
    public sealed record ColumnDef(string Header, Func<object, string?> GetValue, double? FixedWidth = null);

    public static byte[] Export(string title, IReadOnlyList<ColumnDef> columns, IEnumerable<object> rows)
    {
        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        var sheetName = title.Length <= 31 ? title : title[..31];
        var ws = workbook.Worksheets.Add(sheetName);

        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, columns.Count).Merge();

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        var rowIdx = 3;
        foreach (var item in rows)
        {
            for (var c = 0; c < columns.Count; c++)
                ws.Cell(rowIdx, c + 1).Value = columns[c].GetValue(item) ?? string.Empty;

            if (rowIdx % 2 == 0)
            {
                ws.Range(rowIdx, 1, rowIdx, columns.Count)
                    .Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            rowIdx++;
        }

        ws.SheetView.FreezeRows(2);
        ws.Columns(1, columns.Count).AdjustToContents(1, Math.Min(rowIdx - 1, 200));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

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
