using ClosedXML.Excel;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectForecastExportService : IProjectForecastExportService
{
    public Task ExportToExcelAsync(ProjectForecastReportDto report, string filePath)
    {
        return Task.Run(() => BuildExcel(report, filePath));
    }

    public Task ExportToPdfAsync(ProjectForecastReportDto report, string filePath)
    {
        return Task.Run(() => BuildPdf(report, filePath));
    }

    private static void BuildExcel(ProjectForecastReportDto report, string filePath)
    {
        using var wb = new XLWorkbook();

        AddScenariosSheet(wb, report);
        AddVolumesSheet(wb, report);
        AddTop3Sheet(wb, report);

        wb.SaveAs(filePath);
    }

    // ─── Лист 1: Сценарии трудозатрат ────────────────────────────────────────

    private static void AddScenariosSheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("Сценарии");

        ws.Cell(1, 1).Value = $"Прогноз трудозатрат: {report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Сценарий", "Z-оценка", "Трудозатраты, ч", "% от базового", "Интерпретация" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var s in report.Scenarios)
        {
            ws.Cell(row, 1).Value = s.ScenarioName;
            ws.Cell(row, 2).Value = s.ZScore;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Value = s.TotalLaborHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Интерпретации
        row++;
        ws.Cell(row, 1).Value = "Интерпретация сценариев";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 5).Merge();
        row++;

        var intHeaders = new[] { "Сценарий", "Трудозатраты, ч", "% от базового", "Интерпретация" };
        WriteHeaderRow(ws, row, intHeaders);
        row++;

        foreach (var i in report.Interpretations)
        {
            ws.Cell(row, 1).Value = i.ScenarioName;
            ws.Cell(row, 2).Value = i.LaborHours;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Value = i.PercentOfBase;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 4).Value = i.Interpretation;
            row++;
        }

        ws.Column(1).Width = 22;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 40;
        ws.Columns(2, 3).AdjustToContents();
        ws.SheetView.FreezeRows(2);
    }

    // ─── Лист 2: Плановые объёмы ─────────────────────────────────────────────

    private static void AddVolumesSheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("Плановые объёмы");

        ws.Cell(1, 1).Value = $"Плановые объёмы: {report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 8).Merge();

        var allRows = report.Scenarios.FirstOrDefault()?.Disciplines
            ?? report.PlannedVolumes.Select(v => new ForecastIicVolumeRowDto(
                v.WorkDirectionId, v.ParentId, v.Name, v.UnitName,
                v.NormProductivity, v.NormEstimate, v.NormStandardDeviation,
                v.PlannedVolume, v.OptimisticHours, v.BaseHours, v.PessimisticHours,
                v.Range, v.HierarchyPath)).ToList();

        var headers = new[] { "Направление ИИС", "Ед. изм.", "Норм. произв.", "Плановый объём", "Базовый, ч", "Оптимист., ч", "Пессимист., ч", "Диапазон" };
        WriteHeaderRow(ws, 2, headers);

        var idToLevel = BuildLevelMap(report.PlannedVolumes);
        var row = 3;
        foreach (var v in report.PlannedVolumes)
        {
            var level = idToLevel.TryGetValue(v.WorkDirectionId, out var l) ? l : 0;
            var indent = new string(' ', level * 2);
            ws.Cell(row, 1).Value = indent + v.Name;
            ws.Cell(row, 2).Value = v.UnitName ?? string.Empty;
            SetDecimal(ws.Cell(row, 3), v.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 4), v.PlannedVolume > 0 ? v.PlannedVolume : null, "N2");
            SetDecimal(ws.Cell(row, 5), v.BaseHours > 0 ? v.BaseHours : null, "N2");
            SetDecimal(ws.Cell(row, 6), v.OptimisticHours > 0 ? v.OptimisticHours : null, "N2");
            SetDecimal(ws.Cell(row, 7), v.PessimisticHours > 0 ? v.PessimisticHours : null, "N2");
            ws.Cell(row, 8).Value = v.Range;

            if (level == 0)
                ws.Row(row).Style.Font.Bold = true;
            else if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            row++;
        }

        var tableRange = ws.Range(2, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 45;
        ws.Column(2).Width = 12;
        ws.Column(8).Width = 18;
        for (var c = 3; c <= 7; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);
    }

    // ─── Лист 3: ТОП-3 дисциплин ─────────────────────────────────────────────

    private static void AddTop3Sheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("ТОП дисциплин");

        ws.Cell(1, 1).Value = "ТОП дисциплин по трудозатратам";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 3).Merge();

        var headers = new[] { "Место", "Дисциплина", "Базовые трудозатраты, ч" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var t in report.Top3Disciplines)
        {
            ws.Cell(row, 1).Value = t.Rank;
            ws.Cell(row, 2).Value = t.DisciplineName;
            ws.Cell(row, 3).Value = t.BaseHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 45;
        ws.Column(3).AdjustToContents();
        ws.Range(2, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
    }

    private static void SetDecimal(IXLCell cell, decimal? value, string format)
    {
        if (!value.HasValue) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = format == "N4" ? "#,##0.0000" : "#,##0.00";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static Dictionary<int, int> BuildLevelMap(IReadOnlyList<ForecastIicVolumeRowDto> rows)
    {
        var parentMap = rows.ToDictionary(r => r.WorkDirectionId, r => r.ParentId);
        var levels = new Dictionary<int, int>();

        int GetLevel(int id)
        {
            if (levels.TryGetValue(id, out var cached)) return cached;
            if (!parentMap.TryGetValue(id, out var pid) || pid is null)
            {
                levels[id] = 0;
                return 0;
            }
            var level = GetLevel(pid.Value) + 1;
            levels[id] = level;
            return level;
        }

        foreach (var r in rows) GetLevel(r.WorkDirectionId);
        return levels;
    }

    // ─── PDF ─────────────────────────────────────────────────────────────────

    private static void BuildPdf(ProjectForecastReportDto report, string filePath)
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new ForecastWindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Прогноз трудозатрат: {report.ProjectName}";
        doc.Info.Author = "EngineeringPerformance";

        AddForecastSummaryPage(doc, report);

        foreach (var v in report.PlannedVolumes.Where(v => v.ParentId == null || v.PlannedVolume > 0))
            AddForecastDirectionPage(doc, v, report);

        doc.Save(filePath);
    }

    private static void AddForecastSummaryPage(PdfDocument doc, ProjectForecastReportDto report)
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

        gfx.DrawString($"Прогноз трудозатрат: {report.ProjectName} ({report.VersionName})",
            fontTitle, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 20), XStringFormats.TopLeft);
        y += 24;
        gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
            fontBody, XBrushes.Gray, new XRect(margin, y, pageW - margin * 2, 14), XStringFormats.TopLeft);
        y += 20;

        double[] colW = [50, 230, 60, 70, 80, 80, 80];
        string[] colH = ["Шифр", "Направление ИИС", "Ед.изм.", "Факт ФО", "Базовый, ч", "Оптим., ч", "Пессим., ч"];

        var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
        DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
        y += 14;

        var idToLevel = BuildLevelMap(report.PlannedVolumes);
        var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        var rowIdx = 0;

        foreach (var v in report.PlannedVolumes)
        {
            if (y + 13 > page.Height.Point - margin) break;
            var level = idToLevel.TryGetValue(v.WorkDirectionId, out var l) ? l : 0;
            var indent = new string(' ', level * 2);
            var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
            string[] vals =
            [
                string.Empty,
                indent + TruncatePdf(v.Name, 42),
                v.UnitName ?? "—",
                v.PlannedVolume > 0 ? v.PlannedVolume.ToString("N2") : "—",
                v.BaseHours > 0 ? v.BaseHours.ToString("N1") : "—",
                v.OptimisticHours > 0 ? v.OptimisticHours.ToString("N1") : "—",
                v.PessimisticHours > 0 ? v.PessimisticHours.ToString("N1") : "—",
            ];
            DrawPdfRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 12);
            y += 12;
            rowIdx++;
        }

        var roots = report.PlannedVolumes.Where(v => v.ParentId == null).ToList();
        var totalBase = roots.Sum(v => v.BaseHours);
        var totalOpt = roots.Sum(v => v.OptimisticHours);
        var totalPes = roots.Sum(v => v.PessimisticHours);
        var totalBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));
        string[] totalVals = ["ИТОГО", "По проекту", "—", "—",
            totalBase.ToString("N1"), totalOpt.ToString("N1"), totalPes.ToString("N1")];
        DrawPdfRow(gfx, margin, y, colW, totalVals, fontTotal, XBrushes.White, totalBg, 14);
    }

    private static void AddForecastDirectionPage(PdfDocument doc, ForecastIicVolumeRowDto v, ProjectForecastReportDto report)
    {
        if (v.PlannedVolume <= 0 && v.BaseHours <= 0) return;

        var page = doc.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont("Arial", 11, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 8, XFontStyleEx.Bold);
        var fontBody = new XFont("Arial", 7, XFontStyleEx.Regular);

        var margin = 30.0;
        var pageW = page.Width.Point;
        var y = margin;

        gfx.DrawString(v.Name, fontTitle, XBrushes.Black,
            new XRect(margin, y, pageW - margin * 2, 16), XStringFormats.TopLeft);
        y += 18;

        gfx.DrawString(
            $"Факт ФО: {(v.PlannedVolume > 0 ? v.PlannedVolume.ToString("N2") : "—")}   " +
            $"Базовый: {(v.BaseHours > 0 ? v.BaseHours.ToString("N1") : "—")} ч   " +
            $"Оптимист.: {(v.OptimisticHours > 0 ? v.OptimisticHours.ToString("N1") : "—")} ч   " +
            $"Пессимист.: {(v.PessimisticHours > 0 ? v.PessimisticHours.ToString("N1") : "—")} ч",
            fontHeader, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 12), XStringFormats.TopLeft);
        y += 20;

        // Дочерние направления
        var children = report.PlannedVolumes.Where(r => r.ParentId == v.WorkDirectionId).ToList();
        if (children.Count > 0)
        {
            gfx.DrawString("Детализация по дочерним направлениям", fontHeader, XBrushes.Black,
                new XRect(margin, y, 300, 12), XStringFormats.TopLeft);
            y += 13;

            double[] colW = [240, 60, 80, 80, 80];
            string[] hdr = ["Направление", "Ед.изм.", "Базовый, ч", "Оптим., ч", "Пессим., ч"];
            var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
            DrawPdfRow(gfx, margin, y, colW, hdr, fontHeader, XBrushes.White, hdrBg, 12);
            y += 12;

            var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
            var rowIdx = 0;
            foreach (var c in children)
            {
                if (y + 11 > page.Height.Point - margin) break;
                var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
                string[] vals =
                [
                    TruncatePdf(c.Name, 40),
                    c.UnitName ?? "—",
                    c.BaseHours > 0 ? c.BaseHours.ToString("N1") : "—",
                    c.OptimisticHours > 0 ? c.OptimisticHours.ToString("N1") : "—",
                    c.PessimisticHours > 0 ? c.PessimisticHours.ToString("N1") : "—",
                ];
                DrawPdfRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 11);
                y += 11;
                rowIdx++;
            }
        }
    }

    private static void DrawPdfRow(
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
            gfx.DrawString(text, font, textBrush,
                new XRect(cx + 2, y + 1, w - 4, rowH - 2), XStringFormats.TopLeft);
            cx += w;
        }
    }

    private static string TruncatePdf(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "…";
}

file sealed class ForecastWindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static readonly Dictionary<string, string> FaceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Arial",    "arial.ttf"   },
        { "Arial#b",  "arialbd.ttf" },
        { "Arial#i",  "ariali.ttf"  },
        { "Arial#bi", "arialbi.ttf" },
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var key = familyName + (bold && italic ? "#bi" : bold ? "#b" : italic ? "#i" : "");
        if (!FaceMap.ContainsKey(key)) key = familyName;
        if (!FaceMap.ContainsKey(key)) key = "Arial";
        return new FontResolverInfo(FaceMap[key]);
    }

    public byte[]? GetFont(string faceName)
    {
        var path = Path.Combine(FontsDir, faceName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
