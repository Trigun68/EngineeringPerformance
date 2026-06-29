using ClosedXML.Excel;

namespace EngineeringPerformance.App.Services;

/// <summary>
/// Экспорт справочника в Excel: подкрашенный заголовок, закреплённая строка,
/// авто-ширина колонок, форматирование единообразно с существующими выгрузками.
/// </summary>
public static class ReferenceExcelExporter
{
    public sealed record ColumnDef(string Header, Func<object, string?> GetValue, double? FixedWidth = null);

    /// <param name="title">Название листа и заголовок в ячейке A1.</param>
    /// <param name="columns">Описание колонок: заголовок + функция извлечения строки.</param>
    /// <param name="rows">Строки данных (элементы справочника как object).</param>
    /// <param name="filePath">Путь к сохраняемому .xlsx файлу.</param>
    public static void Export(
        string title,
        IReadOnlyList<ColumnDef> columns,
        IEnumerable<object> rows,
        string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(title.Length <= 31 ? title : title[..31]);

        // ─── Строка-заголовок отчёта ──────────────────────────────────
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, columns.Count).Merge();

        // ─── Шапка таблицы ────────────────────────────────────────────
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

        // ─── Данные ───────────────────────────────────────────────────
        var rowIdx = 3;
        foreach (var item in rows)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var rawValue = columns[c].GetValue(item);
                ws.Cell(rowIdx, c + 1).Value = rawValue ?? string.Empty;
            }

            if (rowIdx % 2 == 0)
            {
                ws.Range(rowIdx, 1, rowIdx, columns.Count)
                    .Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            rowIdx++;
        }

        // ─── Рамка таблицы ────────────────────────────────────────────
        if (rowIdx > 2)
        {
            var tableRange = ws.Range(2, 1, rowIdx - 1, columns.Count);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }

        // ─── Ширина колонок ───────────────────────────────────────────
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].FixedWidth.HasValue)
                ws.Column(i + 1).Width = columns[i].FixedWidth!.Value;
            else
                ws.Column(i + 1).AdjustToContents(2, rowIdx - 1, 8, 60);
        }

        // ─── Закрепление первых двух строк ────────────────────────────
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }
}
