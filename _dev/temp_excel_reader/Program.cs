using ClosedXML.Excel;

if (args.Length == 0)
{
    Console.WriteLine("Usage: ExcelReader <path>");
    return 1;
}

var path = args[0];
using var wb = new XLWorkbook(path);

Console.WriteLine($"=== {Path.GetFileName(path)} ===");
foreach (var ws in wb.Worksheets)
{
    Console.WriteLine();
    Console.WriteLine($"Worksheet: '{ws.Name}'");
    var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
    var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
    Console.WriteLine($"  Used range: rows={lastRow}, cols={lastCol}");

    // Print first 30 rows fully (or less) to see structure.
    var rowsToPrint = Math.Min(lastRow, args.Length > 1 ? int.Parse(args[1]) : 30);
    for (var r = 1; r <= rowsToPrint; r++)
    {
        var sb = new System.Text.StringBuilder($"  R{r,3}: ");
        for (var c = 1; c <= lastCol; c++)
        {
            var v = ws.Cell(r, c).GetString();
            if (v.Length > 35) v = v.Substring(0, 32) + "...";
            sb.Append('[').Append(c).Append("]'").Append(v).Append("' ");
        }
        Console.WriteLine(sb.ToString());
    }
}

return 0;
