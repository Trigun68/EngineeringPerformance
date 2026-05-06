using ClosedXML.Excel;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectVersionAnalysisExportService : IProjectVersionAnalysisExportService
{
    public Task ExportToExcelAsync(ProjectVersionAnalysisDto report, string filePath)
    {
        return Task.Run(() => BuildExcel(report, filePath));
    }

    public Task ExportToPdfAsync(ProjectVersionAnalysisDto report, string filePath)
    {
        return Task.Run(() => BuildPdf(report, filePath));
    }

    // ─── Excel ───────────────────────────────────────────────────────────────

    private static void BuildExcel(ProjectVersionAnalysisDto report, string filePath)
    {
        using var wb = new XLWorkbook();

        AddDirectionsSheet(wb, report);
        AddPhysicalVolumesSheet(wb, report);
        AddLaborSheet(wb, report);

        wb.SaveAs(filePath);
    }

    private static void AddDirectionsSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Направления IIC");

        var headers = new[]
        {
            "Шифр", "Направление", "Ед. изм.",
            "Факт ФО", "Факт труд., ч",
            "Факт произв.", "Факт оценка",
            "Норм произв.", "Норм оценка", "Норм СО",
            "Z-оценка", "Интерпретация"
        };

        // Заголовок отчёта
        ws.Cell(1, 1).Value = $"{report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, headers.Length).Merge();

        // Заголовки колонок
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        var row = 3;
        foreach (var d in report.Directions)
        {
            var indent = new string(' ', Math.Max(0, d.Level) * 2);
            var r = ws.Row(row);

            ws.Cell(row, 1).Value = d.Code;
            ws.Cell(row, 2).Value = indent + d.Name;
            ws.Cell(row, 3).Value = d.UnitName ?? string.Empty;
            SetDecimal(ws.Cell(row, 4), d.FactPhysicalVolume, "N2");
            SetDecimal(ws.Cell(row, 5), d.FactLaborHours, "N0");
            SetDecimal(ws.Cell(row, 6), d.FactProductivity, "N4");
            SetDecimal(ws.Cell(row, 7), d.FactEstimate, "N4");
            SetDecimal(ws.Cell(row, 8), d.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 9), d.NormEstimate, "N4");
            SetDecimal(ws.Cell(row, 10), d.NormStdDev, "N4");
            SetDecimal(ws.Cell(row, 11), d.ZScore, "N2");
            ws.Cell(row, 12).Value = d.Interpretation?.ToRussian() ?? string.Empty;

            // Цвет строки по интерпретации
            if (d.Interpretation.HasValue)
            {
                var bgHex = InterpretationToHex(d.Interpretation.Value);
                r.Style.Fill.BackgroundColor = XLColor.FromHtml(bgHex);
            }

            // Чередование фона для нечётных без интерпретации
            if (!d.Interpretation.HasValue && row % 2 == 0)
                r.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            row++;
        }

        // Итого по проекту
        var roots = report.Directions.Where(d => d.ParentId == null).ToList();
        var totalFo = roots.Sum(d => d.FactPhysicalVolume ?? 0m);
        var totalHours = roots.Sum(d => d.FactLaborHours ?? 0m);

        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Cell(row, 2).Value = "По проекту";
        if (totalFo > 0) ws.Cell(row, 4).Value = totalFo;
        if (totalHours > 0) ws.Cell(row, 5).Value = totalHours;
        var totalRow = ws.Row(row);
        totalRow.Style.Font.Bold = true;
        totalRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        totalRow.Style.Font.FontColor = XLColor.White;

        // Форматирование таблицы
        var tableRange = ws.Range(2, 1, row, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(2).Width = 45;
        ws.Column(12).Width = 28;
        for (var c = 1; c <= headers.Length; c++)
        {
            if (c != 2 && c != 12) ws.Column(c).AdjustToContents();
        }
        ws.SheetView.FreezeRows(2);
    }

    private static void AddPhysicalVolumesSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Физ. объёмы");

        ws.Cell(1, 1).Value = "Физические объёмы по направлениям IIC";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 8).Merge();

        var headers = new[] { "Направление", "Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        foreach (var d in report.Directions.Where(d => d.PhysicalVolumes.Count > 0))
        {
            // Строка-заголовок направления
            ws.Cell(row, 1).Value = $"[{d.Code}] {d.Name}";
            ws.Range(row, 1, row, headers.Length).Merge();
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
            row++;

            foreach (var pv in d.PhysicalVolumes)
            {
                ws.Cell(row, 1).Value = string.Empty;
                ws.Cell(row, 2).Value = pv.Code;
                ws.Cell(row, 3).Value = pv.Name;
                ws.Cell(row, 4).Value = pv.UnitName ?? string.Empty;
                ws.Cell(row, 5).Value = pv.Quantity;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00##";
                ws.Cell(row, 6).Value = pv.Coefficient;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.0000";
                ws.Cell(row, 7).Value = pv.Total;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
        }

        ws.Column(1).Width = 4;
        ws.Column(3).Width = 50;
        for (var c = 2; c <= headers.Length; c++)
        {
            if (c != 3) ws.Column(c).AdjustToContents();
        }

        var tableRange = ws.Range(2, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        ws.SheetView.FreezeRows(2);
    }

    private static void AddLaborSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Трудоёмкость");

        ws.Cell(1, 1).Value = "Трудоёмкость по направлениям IIC";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 3).Merge();

        var headers = new[] { "Направление", "Шифр документации", "План, ч" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 3;
        foreach (var d in report.Directions.Where(d => d.DirectionLaborIntensities.Count > 0))
        {
            ws.Cell(row, 1).Value = $"[{d.Code}] {d.Name}";
            ws.Range(row, 1, row, 3).Merge();
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
            row++;

            foreach (var li in d.DirectionLaborIntensities)
            {
                ws.Cell(row, 2).Value = li.DocumentationCode;
                ws.Cell(row, 3).Value = li.PlannedHours;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
        }

        ws.Column(1).Width = 4;
        ws.Column(2).Width = 45;
        ws.Column(3).AdjustToContents();

        var tableRange = ws.Range(2, 1, row - 1, 3);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        ws.SheetView.FreezeRows(2);
    }

    private static void SetDecimal(IXLCell cell, decimal? value, string format)
    {
        if (!value.HasValue) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = format == "N0" ? "#,##0" : "#,##0.00##";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static string InterpretationToHex(PerformanceInterpretation interp) => interp switch
    {
        PerformanceInterpretation.Excellent => "#E8F5E9",
        PerformanceInterpretation.Good => "#F1F8E9",
        PerformanceInterpretation.Satisfactory => "#FFFDE7",
        PerformanceInterpretation.NeedsAttention => "#FFF3E0",
        PerformanceInterpretation.Critical => "#FFEBEE",
        _ => "#FFFFFF"
    };

    // ─── PDF ─────────────────────────────────────────────────────────────────

    private static void BuildPdf(ProjectVersionAnalysisDto report, string filePath)
    {
        // PdfSharp 6.x требует явной регистрации FontResolver под .NET Core/5+.
        // WindowsPlatformFontResolver читает шрифты из системной папки Windows.
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new WindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Анализ версии: {report.ProjectName}";
        doc.Info.Author = "EngineeringPerformance";

        // Одна страница на направление или блоки по страницам
        AddDirectionsSummaryPage(doc, report);

        foreach (var direction in report.Directions.Where(d => d.PhysicalVolumes.Count > 0 || d.DirectionLaborIntensities.Count > 0))
        {
            AddDirectionDetailPage(doc, direction, report);
        }

        doc.Save(filePath);
    }

    private static void AddDirectionsSummaryPage(PdfDocument doc, ProjectVersionAnalysisDto report)
    {
        var page = doc.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 7, XFontStyleEx.Regular);
        var fontTotal = new XFont("Arial", 8, XFontStyleEx.Bold);

        var margin = 30.0;
        var y = margin;
        var pageW = page.Width.Point;

        // Заголовок
        gfx.DrawString($"Анализ версии проекта: {report.ProjectName} ({report.VersionName})",
            fontTitle, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 20), XStringFormats.TopLeft);
        y += 24;
        gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
            fontBody, XBrushes.Gray, new XRect(margin, y, pageW - margin * 2, 14), XStringFormats.TopLeft);
        y += 20;

        // Таблица направлений
        double[] colW = [50, 220, 60, 60, 60, 60, 60, 60, 60, 60, 55, 90];
        string[] colH = ["Шифр", "Направление", "Ед.изм.", "Факт ФО", "Факт ч", "Факт пр.", "Факт оц.", "Норм пр.", "Норм оц.", "Норм СО", "Z-оц.", "Интерпретация"];

        DrawPdfTableRow(gfx, margin, y, colW, colH, fontHeader,
            XBrushes.White, new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50)), 14);
        y += 14;

        var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        var bgWhite = XBrushes.White;

        var roots = report.Directions.Where(d => d.ParentId == null).ToList();
        var totalFo = roots.Sum(d => d.FactPhysicalVolume ?? 0m);
        var totalHours = roots.Sum(d => d.FactLaborHours ?? 0m);

        var rowIdx = 0;
        foreach (var d in report.Directions)
        {
            if (y + 13 > page.Height.Point - margin)
                break;

            var bg = d.Interpretation.HasValue
                ? new XSolidBrush(InterpretationToXColor(d.Interpretation.Value))
                : (rowIdx % 2 == 0 ? bgWhite : bgLight);

            var indent = new string(' ', Math.Max(0, d.Level) * 2);
            string[] vals =
            [
                d.Code,
                indent + TruncateStr(d.Name, 38),
                d.UnitName ?? "—",
                FormatDecimal(d.FactPhysicalVolume, "N2"),
                FormatDecimal(d.FactLaborHours, "N0"),
                FormatDecimal(d.FactProductivity, "N3"),
                FormatDecimal(d.FactEstimate, "N3"),
                FormatDecimal(d.NormProductivity, "N3"),
                FormatDecimal(d.NormEstimate, "N3"),
                FormatDecimal(d.NormStdDev, "N3"),
                FormatDecimal(d.ZScore, "N2"),
                d.Interpretation?.ToRussian() ?? "—"
            ];

            DrawPdfTableRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 12);
            y += 12;
            rowIdx++;
        }

        // Строка ИТОГО
        var totalBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));
        string[] totalVals =
        [
            "ИТОГО", "По проекту", "—",
            totalFo > 0 ? totalFo.ToString("N2") : "—",
            totalHours > 0 ? totalHours.ToString("N0") : "—",
            "—", "—", "—", "—", "—", "—", "—"
        ];
        DrawPdfTableRow(gfx, margin, y, colW, totalVals, fontTotal, XBrushes.White, totalBg, 13);
    }

    private static void AddDirectionDetailPage(PdfDocument doc, ProjectVersionDirectionDto direction, ProjectVersionAnalysisDto report)
    {
        var page = doc.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont("Arial", 11, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 7, XFontStyleEx.Regular);

        var margin = 30.0;
        var pageW = page.Width.Point;
        var y = margin;

        // Заголовок направления
        gfx.DrawString($"[{direction.Code}] {direction.Name}", fontTitle, XBrushes.Black,
            new XRect(margin, y, pageW - margin * 2, 16), XStringFormats.TopLeft);
        y += 18;

        if (!string.IsNullOrEmpty(direction.UnitName))
        {
            gfx.DrawString($"Единица измерения: {direction.UnitName}", fontBody, XBrushes.Gray,
                new XRect(margin, y, pageW - margin * 2, 12), XStringFormats.TopLeft);
            y += 14;
        }

        // Метрики
        var metricsLine = $"Факт ФО: {FormatDecimal(direction.FactPhysicalVolume, "N2")}   " +
                          $"Факт труд.: {FormatDecimal(direction.FactLaborHours, "N0")} ч   " +
                          $"Z-оценка: {FormatDecimal(direction.ZScore, "N2")}   " +
                          $"Интерпретация: {direction.Interpretation?.ToRussian() ?? "—"}";
        gfx.DrawString(metricsLine, fontHeader, XBrushes.Black,
            new XRect(margin, y, pageW - margin * 2, 12), XStringFormats.TopLeft);
        y += 18;

        // Физ. объёмы
        if (direction.PhysicalVolumes.Count > 0)
        {
            gfx.DrawString("Физические объёмы проекта", fontHeader, XBrushes.Black,
                new XRect(margin, y, 200, 12), XStringFormats.TopLeft);
            y += 13;

            double[] pvColW = [50, 300, 55, 60, 55, 70];
            string[] pvHdr = ["Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого"];
            var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
            DrawPdfTableRow(gfx, margin, y, pvColW, pvHdr, fontHeader, XBrushes.White, hdrBg, 12);
            y += 12;

            var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
            var rowIdx = 0;
            foreach (var pv in direction.PhysicalVolumes)
            {
                if (y + 11 > page.Height.Point - margin) break;
                var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
                string[] vals = [pv.Code, TruncateStr(pv.Name, 52), pv.UnitName ?? "—",
                    pv.Quantity.ToString("N2"), pv.Coefficient.ToString("N4"), pv.Total.ToString("N2")];
                DrawPdfTableRow(gfx, margin, y, pvColW, vals, fontBody, XBrushes.Black, bg, 11);
                y += 11;
                rowIdx++;
            }
            y += 8;
        }

        // Трудоёмкость по направлению
        if (direction.DirectionLaborIntensities.Count > 0 && y + 40 < page.Height.Point - margin)
        {
            gfx.DrawString("Трудоёмкость по направлению", fontHeader, XBrushes.Black,
                new XRect(margin, y, 200, 12), XStringFormats.TopLeft);
            y += 13;

            double[] lColW = [500, 90];
            string[] lHdr = ["Шифр документации", "План, ч"];
            var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
            DrawPdfTableRow(gfx, margin, y, lColW, lHdr, fontHeader, XBrushes.White, hdrBg, 12);
            y += 12;

            var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
            var rowIdx = 0;
            foreach (var li in direction.DirectionLaborIntensities)
            {
                if (y + 11 > page.Height.Point - margin) break;
                var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
                string[] vals = [TruncateStr(li.DocumentationCode, 85), li.PlannedHours.ToString("N2")];
                DrawPdfTableRow(gfx, margin, y, lColW, vals, fontBody, XBrushes.Black, bg, 11);
                y += 11;
                rowIdx++;
            }
        }
    }

    private static void DrawPdfTableRow(
        XGraphics gfx, double x, double y,
        double[] colWidths, string[] values,
        XFont font, XBrush textBrush, XBrush bgBrush, double rowH)
    {
        var cx = x;
        for (var i = 0; i < colWidths.Length; i++)
        {
            var w = colWidths[i];
            gfx.DrawRectangle(bgBrush, cx, y, w, rowH);
            gfx.DrawRectangle(XPens.LightGray, cx, y, w, rowH);
            var text = i < values.Length ? values[i] : string.Empty;
            var rect = new XRect(cx + 2, y + 1, w - 4, rowH - 2);
            gfx.DrawString(text, font, textBrush, rect, XStringFormats.TopLeft);
            cx += w;
        }
    }

    private static string FormatDecimal(decimal? value, string format) =>
        value.HasValue ? value.Value.ToString(format) : "—";

    private static string TruncateStr(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "…";

    private static XColor InterpretationToXColor(PerformanceInterpretation interp) => interp switch
    {
        PerformanceInterpretation.Excellent => XColor.FromArgb(0xFF, 0xE8, 0xF5, 0xE9),
        PerformanceInterpretation.Good => XColor.FromArgb(0xFF, 0xF1, 0xF8, 0xE9),
        PerformanceInterpretation.Satisfactory => XColor.FromArgb(0xFF, 0xFF, 0xFD, 0xE7),
        PerformanceInterpretation.NeedsAttention => XColor.FromArgb(0xFF, 0xFF, 0xF3, 0xE0),
        PerformanceInterpretation.Critical => XColor.FromArgb(0xFF, 0xFF, 0xEB, 0xEE),
        _ => XColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
    };
}

/// <summary>
/// Читает шрифты из системной папки Windows для PdfSharp 6.x под .NET Core/5+.
/// </summary>
file sealed class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static readonly Dictionary<string, string> FaceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Arial",              "arial.ttf"   },
        { "Arial#b",            "arialbd.ttf" },
        { "Arial#i",            "ariali.ttf"  },
        { "Arial#bi",           "arialbi.ttf" },
        { "Segoe UI",           "segoeui.ttf"  },
        { "Segoe UI#b",         "segoeuib.ttf" },
        { "Segoe UI#i",         "segoeuii.ttf" },
        { "Times New Roman",    "times.ttf"    },
        { "Courier New",        "cour.ttf"     },
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var key = familyName + (bold && italic ? "#bi" : bold ? "#b" : italic ? "#i" : "");
        if (!FaceMap.ContainsKey(key))
            key = familyName;
        if (!FaceMap.ContainsKey(key))
            key = "Arial";

        return new FontResolverInfo(FaceMap[key]);
    }

    public byte[]? GetFont(string faceName)
    {
        var path = Path.Combine(FontsDir, faceName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
