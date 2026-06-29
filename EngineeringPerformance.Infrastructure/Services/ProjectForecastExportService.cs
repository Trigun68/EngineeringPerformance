using ClosedXML.Excel;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectForecastExportService : IProjectForecastExportService
{
    public Task ExportToExcelAsync(ProjectVersionAnalysisDto report, string filePath)
        => Task.Run(() => BuildExcel(report, filePath));

    public Task ExportToPdfAsync(ProjectVersionAnalysisDto report, string filePath)
        => Task.Run(() => BuildPdf(report, filePath));

    public Task ExportForecastSummaryToExcelAsync(IReadOnlyList<ForecastSummaryRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildForecastSummaryExcel(rows, projectCode, versionName, filePath));

    public Task ExportForecastSummaryToPdfAsync(IReadOnlyList<ForecastSummaryRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildForecastSummaryPdf(rows, projectCode, versionName, filePath));

    public Task ExportFacilityForecastToExcelAsync(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildFacilityExcel(rows, projectCode, versionName, filePath));

    public Task ExportFacilityForecastToPdfAsync(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildFacilityPdf(rows, projectCode, versionName, filePath));

    public Task ExportFactFacilityToExcelAsync(IReadOnlyList<FactFacilityRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildFactFacilityExcel(rows, projectCode, versionName, filePath));

    public Task ExportFactFacilityToPdfAsync(IReadOnlyList<FactFacilityRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildFactFacilityPdf(rows, projectCode, versionName, filePath));

    public Task ExportFactDetailToExcelAsync(IReadOnlyList<FactDetailRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildFactDetailExcel(rows, projectCode, versionName, filePath));

    public Task ExportDetailForecastToExcelAsync(IReadOnlyList<DetailForecastRowDto> rows, string projectCode, string versionName, string filePath)
        => Task.Run(() => BuildDetailExcel(rows, projectCode, versionName, filePath));

    public Task ExportDetailForecastSummaryTableToExcelAsync(DetailForecastSummaryTableDto table, string filePath)
        => Task.Run(() => BuildDetailSummaryTableExcel(table, filePath));

    // ─── Excel Итого (ForecastSummary) ──────────────────────────────────────

    private static void BuildForecastSummaryExcel(IReadOnlyList<ForecastSummaryRowDto> rows, string projectCode, string versionName, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Итого");

        ws.Cell(1, 1).Value = $"Прогноз трудозатрат (Итого): {projectCode} ({versionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 8).Merge();

        var headers = new[] { "Шифр", "Направление ИИС", "Ед. изм.", "Факт ФО", "Норм. произв.", "Базовый, ч", "Оптимист., ч", "Пессимист., ч" };
        WriteHeaderRow(ws, 2, headers);

        var dataRow = 3;
        foreach (var r in rows)
        {
            if (r.IsTotal && r.Level == -1)
            {
                ws.Cell(dataRow, 1).Value = "ИТОГО";
                ws.Cell(dataRow, 2).Value = "По проекту";
                SetDecimal(ws.Cell(dataRow, 6), r.BaseHours, "N2");
                SetDecimal(ws.Cell(dataRow, 7), r.OptimisticHours, "N2");
                SetDecimal(ws.Cell(dataRow, 8), r.PessimisticHours, "N2");
                ws.Row(dataRow).Style.Font.Bold = true;
                ws.Row(dataRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
                ws.Row(dataRow).Style.Font.FontColor = XLColor.White;
            }
            else if (r.IsTotal)
            {
                ws.Cell(dataRow, 1).Value = string.Empty;
                ws.Cell(dataRow, 2).Value = r.DisplayName;
                ws.Cell(dataRow, 3).Value = r.NormUnitName;
                SetDecimal(ws.Cell(dataRow, 6), r.BaseHours, "N2");
                SetDecimal(ws.Cell(dataRow, 7), r.OptimisticHours, "N2");
                SetDecimal(ws.Cell(dataRow, 8), r.PessimisticHours, "N2");
                ws.Row(dataRow).Style.Font.Bold = true;
                ws.Row(dataRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
            }
            else
            {
                ws.Cell(dataRow, 1).Value = r.Code;
                ws.Cell(dataRow, 2).Value = r.DisplayName;
                ws.Cell(dataRow, 3).Value = r.NormUnitName;
                SetDecimal(ws.Cell(dataRow, 4), r.FactPhysicalVolume, "N2");
                SetDecimal(ws.Cell(dataRow, 5), r.NormProductivity, "N4");
                SetDecimal(ws.Cell(dataRow, 6), r.BaseHours, "N2");
                SetDecimal(ws.Cell(dataRow, 7), r.OptimisticHours, "N2");
                SetDecimal(ws.Cell(dataRow, 8), r.PessimisticHours, "N2");
                if (r.Level == 0)
                    ws.Row(dataRow).Style.Font.Bold = true;
                else if (dataRow % 2 == 0)
                    ws.Row(dataRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }
            dataRow++;
        }

        var tableRange = ws.Range(2, 1, dataRow - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 45;
        ws.Column(3).Width = 10;
        for (var c = 4; c <= headers.Length; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }

    // ─── PDF Итого (ForecastSummary) ────────────────────────────────────────

    private static void BuildForecastSummaryPdf(IReadOnlyList<ForecastSummaryRowDto> rows, string projectCode, string versionName, string filePath)
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new ForecastWindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Прогноз трудозатрат (Итого): {projectCode}";

        var fontTitle  = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial",  8, XFontStyleEx.Bold);
        var fontBody   = new XFont("Arial",  7, XFontStyleEx.Regular);
        var fontBold   = new XFont("Arial",  7, XFontStyleEx.Bold);
        var fontTotal  = new XFont("Arial",  8, XFontStyleEx.Bold);

        var margin = 30.0;
        double[] colW = [50, 260, 45, 65, 70, 80, 80, 80];
        string[] colH = ["Шифр", "Направление ИИС", "Ед. изм.", "Факт ФО", "Норм. произв.", "Базовый, ч", "Оптимист., ч", "Пессимист., ч"];

        var hdrBg    = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
        var bgLight  = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        var bgSubtot = new XSolidBrush(XColor.FromArgb(0xFF, 0xEE, 0xF4, 0xFF));
        var bgTotalB = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));

        PdfPage AddPage()
        {
            var p = doc.AddPage();
            p.Orientation = PdfSharp.PageOrientation.Landscape;
            return p;
        }

        var page = AddPage();
        var gfx  = XGraphics.FromPdfPage(page);
        var pageW = page.Width.Point;
        var pageH = page.Height.Point;
        var y = margin;

        gfx.DrawString($"Прогноз трудозатрат (Итого): {projectCode} ({versionName})",
            fontTitle, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 20), XStringFormats.TopLeft);
        y += 24;
        gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
            fontBody, XBrushes.Gray, new XRect(margin, y, pageW - margin * 2, 14), XStringFormats.TopLeft);
        y += 20;

        DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
        y += 14;

        var rowIdx = 0;

        void EnsureSpace(double rowH)
        {
            if (y + rowH <= pageH - margin) return;
            gfx.Dispose();
            page = AddPage();
            gfx  = XGraphics.FromPdfPage(page);
            y    = margin;
            DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
            y += 14;
        }

        foreach (var r in rows)
        {
            if (r.IsTotal && r.Level == -1)
            {
                EnsureSpace(14);
                string[] vals = ["ИТОГО", "По проекту", "", "",
                    r.BaseHours.HasValue        ? r.BaseHours.Value.ToString("N1")        : "—",
                    r.OptimisticHours.HasValue  ? r.OptimisticHours.Value.ToString("N1")  : "—",
                    r.PessimisticHours.HasValue ? r.PessimisticHours.Value.ToString("N1") : "—",
                    string.Empty];
                DrawPdfRow(gfx, margin, y, colW, vals, fontTotal, XBrushes.White, bgTotalB, 14);
                y += 14;
            }
            else if (r.IsTotal)
            {
                EnsureSpace(12);
                string[] vals = [string.Empty, TruncatePdf(r.DisplayName, 42), "", "", "",
                    r.BaseHours.HasValue        ? r.BaseHours.Value.ToString("N1")        : "—",
                    r.OptimisticHours.HasValue  ? r.OptimisticHours.Value.ToString("N1")  : "—",
                    r.PessimisticHours.HasValue ? r.PessimisticHours.Value.ToString("N1") : "—"];
                DrawPdfRow(gfx, margin, y, colW, vals, fontBold, XBrushes.Black, bgSubtot, 12);
                y += 12;
            }
            else
            {
                EnsureSpace(12);
                var bg   = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
                var font = r.Level == 0 ? fontBold : fontBody;
                string[] vals =
                [
                    r.Code,
                    TruncatePdf(r.DisplayName, 40),
                    r.NormUnitName,
                    r.FactPhysicalVolume.HasValue  ? r.FactPhysicalVolume.Value.ToString("N2")  : "—",
                    r.NormProductivity.HasValue    ? r.NormProductivity.Value.ToString("N4")    : "—",
                    r.BaseHours.HasValue           ? r.BaseHours.Value.ToString("N1")           : "—",
                    r.OptimisticHours.HasValue     ? r.OptimisticHours.Value.ToString("N1")     : "—",
                    r.PessimisticHours.HasValue    ? r.PessimisticHours.Value.ToString("N1")    : "—",
                ];
                DrawPdfRow(gfx, margin, y, colW, vals, font, XBrushes.Black, bg, 12);
                y += 12;
                rowIdx++;
            }
        }

        gfx.Dispose();
        doc.Save(filePath);
    }

    // ─── Excel по объектам ───────────────────────────────────────────────────

    private static void BuildFacilityExcel(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("По объектам");

        ws.Cell(1, 1).Value = $"Прогноз трудозатрат по объектам: {projectCode} ({versionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Шифр объекта", "Наименование объекта", "Базовый, ч", "Оптимист., ч", "Пессимист., ч" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.FacilityCode;
            ws.Cell(row, 2).Value = r.FacilityName;
            SetDecimal(ws.Cell(row, 3), r.BaseHours, "N2");
            SetDecimal(ws.Cell(row, 4), r.OptimisticHours, "N2");
            SetDecimal(ws.Cell(row, 5), r.PessimisticHours, "N2");
            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            totalBase += r.BaseHours ?? 0m;
            totalOpt  += r.OptimisticHours ?? 0m;
            totalPes  += r.PessimisticHours ?? 0m;
            row++;
        }

        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Cell(row, 2).Value = string.Empty;
        SetDecimal(ws.Cell(row, 3), totalBase, "N2");
        SetDecimal(ws.Cell(row, 4), totalOpt, "N2");
        SetDecimal(ws.Cell(row, 5), totalPes, "N2");
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        ws.Row(row).Style.Font.FontColor = XLColor.White;

        var tableRange = ws.Range(2, 1, row, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 18;
        ws.Column(2).Width = 50;
        for (var c = 3; c <= headers.Length; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }

    // ─── PDF по объектам ─────────────────────────────────────────────────────

    private static void BuildFacilityPdf(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath)
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new ForecastWindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Прогноз по объектам: {projectCode}";

        var fontTitle  = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial",  8, XFontStyleEx.Bold);
        var fontBody   = new XFont("Arial",  7, XFontStyleEx.Regular);
        var fontTotal  = new XFont("Arial",  8, XFontStyleEx.Bold);

        var margin = 30.0;
        double[] colW = [80, 360, 90, 90, 90];
        string[] colH = ["Шифр объекта", "Наименование объекта", "Базовый, ч", "Оптимист., ч", "Пессимист., ч"];

        var hdrBg   = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
        var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        var totalBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));

        // Итоги по всем строкам — как в Excel и TotalSummary грида, не только по видимой странице.
        var totalBase = rows.Sum(r => r.BaseHours ?? 0m);
        var totalOpt  = rows.Sum(r => r.OptimisticHours ?? 0m);
        var totalPes  = rows.Sum(r => r.PessimisticHours ?? 0m);

        PdfPage AddPage()
        {
            var p = doc.AddPage();
            p.Orientation = PdfSharp.PageOrientation.Landscape;
            return p;
        }

        var page = AddPage();
        var gfx  = XGraphics.FromPdfPage(page);
        var pageW = page.Width.Point;
        var pageH = page.Height.Point;
        var y = margin;

        gfx.DrawString($"Прогноз трудозатрат по объектам: {projectCode} ({versionName})",
            fontTitle, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 20), XStringFormats.TopLeft);
        y += 24;
        gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
            fontBody, XBrushes.Gray, new XRect(margin, y, pageW - margin * 2, 14), XStringFormats.TopLeft);
        y += 20;

        DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
        y += 14;

        var rowIdx = 0;

        void EnsureSpace(double rowH)
        {
            if (y + rowH <= pageH - margin) return;
            gfx.Dispose();
            page = AddPage();
            gfx  = XGraphics.FromPdfPage(page);
            pageH = page.Height.Point;
            y    = margin;
            DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
            y += 14;
        }

        foreach (var r in rows)
        {
            EnsureSpace(12);
            var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
            string[] vals =
            [
                r.FacilityCode,
                r.FacilityName,
                r.BaseHours.HasValue        ? r.BaseHours.Value.ToString("N2")        : "—",
                r.OptimisticHours.HasValue  ? r.OptimisticHours.Value.ToString("N2")  : "—",
                r.PessimisticHours.HasValue ? r.PessimisticHours.Value.ToString("N2") : "—",
            ];
            DrawPdfRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 12);
            y += 12;
            rowIdx++;
        }

        EnsureSpace(14);
        string[] totalVals =
        [
            "ИТОГО", string.Empty,
            totalBase > 0 ? totalBase.ToString("N2") : "—",
            totalOpt  > 0 ? totalOpt.ToString("N2")  : "—",
            totalPes  > 0 ? totalPes.ToString("N2")  : "—",
        ];
        DrawPdfRow(gfx, margin, y, colW, totalVals, fontTotal, XBrushes.White, totalBg, 14);

        gfx.Dispose();
        doc.Save(filePath);
    }



    // ─── Excel «По объектам» (Оценка факта) ──────────────────────────────────

    private static void BuildFactFacilityExcel(IReadOnlyList<FactFacilityRowDto> rows, string projectCode, string versionName, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("По объектам");

        ws.Cell(1, 1).Value = $"Оценка факта по объектам: {projectCode} ({versionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 6).Merge();

        var headers = new[] { "Код", "Наименование", "Факт.Трудоёмк., ч", "Z-score", "Перцентиль", "Интерпретация" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        decimal totalHours = 0m;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Code;
            ws.Cell(row, 2).Value = r.Name;
            SetDecimal(ws.Cell(row, 3), r.FactLaborHours, "N2");
            SetDecimal(ws.Cell(row, 4), r.ZScore, "N4");
            ws.Cell(row, 5).Value = r.Percentile;
            ws.Cell(row, 6).Value = r.Interpretation;
            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            totalHours += r.FactLaborHours ?? 0m;
            row++;
        }

        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Cell(row, 2).Value = string.Empty;
        SetDecimal(ws.Cell(row, 3), totalHours, "N2");
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        ws.Row(row).Style.Font.FontColor = XLColor.White;

        var tableRange = ws.Range(2, 1, row, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 50;
        ws.Column(6).Width = 38;
        for (var c = 3; c <= 5; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }

    // ─── PDF «По объектам» (Оценка факта) ────────────────────────────────────

    private static void BuildFactFacilityPdf(IReadOnlyList<FactFacilityRowDto> rows, string projectCode, string versionName, string filePath)
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new ForecastWindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Оценка факта по объектам: {projectCode}";

        var page = doc.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle  = new XFont("Arial", 13, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial",  8, XFontStyleEx.Bold);
        var fontBody   = new XFont("Arial",  7, XFontStyleEx.Regular);
        var fontTotal  = new XFont("Arial",  8, XFontStyleEx.Bold);

        var margin = 30.0;
        var pageW  = page.Width.Point;
        var y = margin;

        gfx.DrawString($"Оценка факта по объектам: {projectCode} ({versionName})",
            fontTitle, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 20), XStringFormats.TopLeft);
        y += 24;
        gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
            fontBody, XBrushes.Gray, new XRect(margin, y, pageW - margin * 2, 14), XStringFormats.TopLeft);
        y += 20;

        double[] colW = [70, 280, 90, 70, 80, 170];
        string[] colH = ["Код", "Наименование", "Факт.Трудоёмк., ч", "Z-score", "Перцентиль", "Интерпретация"];

        var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
        DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
        y += 14;

        var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        decimal totalHours = 0m;
        var rowIdx = 0;

        foreach (var r in rows)
        {
            if (y + 12 > page.Height.Point - margin) break;
            totalHours += r.FactLaborHours ?? 0m;

            var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
            string[] vals =
            [
                r.Code,
                TruncatePdf(r.Name, 48),
                r.FactLaborHours.HasValue ? r.FactLaborHours.Value.ToString("N2") : "—",
                r.ZScore.HasValue         ? r.ZScore.Value.ToString("N4")         : "—",
                r.Percentile,
                TruncatePdf(r.Interpretation, 30),
            ];
            DrawPdfRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 12);
            y += 12;
            rowIdx++;
        }

        var totalBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));
        string[] totalVals =
        [
            "ИТОГО", string.Empty,
            totalHours > 0 ? totalHours.ToString("N2") : "—",
            string.Empty, string.Empty, string.Empty,
        ];
        DrawPdfRow(gfx, margin, y, colW, totalVals, fontTotal, XBrushes.White, totalBg, 14);

        doc.Save(filePath);
    }



    // ─── Excel «Детально» (Оценка факта) ─────────────────────────────────────

    private static void BuildFactDetailExcel(IReadOnlyList<FactDetailRowDto> rows, string projectCode, string versionName, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Детально");

        ws.Cell(1, 1).Value = $"Оценка факта (детально): {projectCode} ({versionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 21).Merge();

        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта",
            "Тип привязки", "Комплект документов", "ID по каталогу", "TAG",
            "ID работы", "Наименование", "Комментарий", "Описательная часть",
            "Код ФО", "Наименование ФО",
            "Группа ФО IIC", "Ед. изм.",
            "Кол-во",
            "Факт.Трудоёмк., ч", "Факт производит.", "Норм производит.",
            "Z-score", "Перцентиль", "Интерпретация"
        };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        decimal totalHours = 0m;
        string? lastFacility = null;

        foreach (var r in rows)
        {
            var facKey = r.FacilityCode + "|" + r.FacilityName;
            if (facKey != lastFacility)
            {
                var hdr = string.IsNullOrEmpty(r.FacilityCode)
                    ? r.FacilityName
                    : $"{r.FacilityCode} — {r.FacilityName}";
                ws.Cell(row, 1).Value = hdr;
                ws.Range(row, 1, row, headers.Length).Merge();
                ws.Row(row).Style.Font.Bold = true;
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
                row++;
                lastFacility = facKey;
            }

            ws.Cell(row, 1).Value = r.FacilityCode;
            ws.Cell(row, 2).Value = r.FacilityName;
            ws.Cell(row, 3).Value = r.BindingType;
            ws.Cell(row, 4).Value = r.DocumentSet;
            ws.Cell(row, 5).Value = r.CatalogId;
            ws.Cell(row, 6).Value = r.Tag;
            ws.Cell(row, 7).Value = r.WorkId;
            ws.Cell(row, 8).Value = r.WorkName;
            ws.Cell(row, 9).Value = r.WorkComment;
            ws.Cell(row, 10).Value = r.EstimationBasis;
            ws.Cell(row, 11).Value = r.PvCode;
            ws.Cell(row, 12).Value = r.PvName;
            ws.Cell(row, 13).Value = r.IicGroupName;
            ws.Cell(row, 14).Value = r.IicUnitName;
            ws.Cell(row, 15).Value = r.Quantity;
            ws.Cell(row, 15).Style.NumberFormat.Format = "#,##0.00##";
            ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            SetDecimal(ws.Cell(row, 16), r.FactLaborHours, "N2");
            SetDecimal(ws.Cell(row, 17), r.FactProductivity, "N4");
            SetDecimal(ws.Cell(row, 18), r.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 19), r.ZScore, "N4");
            ws.Cell(row, 20).Value = r.Percentile;
            ws.Cell(row, 21).Value = r.Interpretation;

            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            totalHours += r.FactLaborHours ?? 0m;
            row++;
        }

        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Range(row, 1, row, 15).Merge();
        SetDecimal(ws.Cell(row, 16), totalHours, "N2");
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        ws.Row(row).Style.Font.FontColor = XLColor.White;

        ws.Range(2, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(2).Width = 35;
        ws.Column(8).Width = 30;
        ws.Column(12).Width = 30;
        ws.Column(13).Width = 25;
        ws.Column(21).Width = 30;
        for (var c = 1; c <= headers.Length; c++)
            if (c != 2 && c != 8 && c != 12 && c != 13 && c != 21) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }



    // ─── Вычисление прогнозных часов (та же формула, что в ForecastDirectionViewModel) ──

    private static (decimal Base, decimal Opt, decimal Pes) CalcHours(ProjectVersionDirectionDto d)
    {
        if (!d.FactPhysicalVolume.HasValue || !d.NormProductivity.HasValue || d.NormProductivity.Value <= 0)
            return (0m, 0m, 0m);

        var fo = d.FactPhysicalVolume.Value;
        var p = d.NormProductivity.Value;
        var baseH = Math.Round(fo * p, 2);

        if (d.NormStdDev.HasValue && d.NormStdDev.Value > 0)
        {
            var std = d.NormStdDev.Value;
            return (baseH,
                Math.Round(fo * Math.Max(0m, p - 0.5m * std), 2),
                Math.Round(fo * (p + 0.5m * std), 2));
        }

        return (baseH, baseH, baseH);
    }

    // ─── Excel ───────────────────────────────────────────────────────────────

    private static void BuildExcel(ProjectVersionAnalysisDto report, string filePath)
    {
        using var wb = new XLWorkbook();
        AddForecastSheet(wb, report);
        AddPhysicalVolumesSheet(wb, report);
        AddEquipmentSheet(wb, report);
        wb.SaveAs(filePath);
    }

    private static void AddForecastSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Прогноз");

        ws.Cell(1, 1).Value = $"Прогноз трудозатрат: {report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 9).Merge();

        var headers = new[] { "Шифр", "Направление ИИС", "Ед. изм.", "Факт ФО", "Норм. произв.", "Норм СО", "Базовый, ч", "Оптимист., ч", "Пессимист., ч" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;

        foreach (var d in report.Directions)
        {
            var indent = new string(' ', Math.Max(0, d.Level) * 2);
            var (baseH, optH, pesH) = CalcHours(d);

            ws.Cell(row, 1).Value = d.Code;
            ws.Cell(row, 2).Value = indent + d.Name;
            ws.Cell(row, 3).Value = d.UnitName ?? string.Empty;
            SetDecimal(ws.Cell(row, 4), d.FactPhysicalVolume, "N2");
            SetDecimal(ws.Cell(row, 5), d.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 6), d.NormStdDev, "N4");
            SetDecimal(ws.Cell(row, 7), baseH > 0 ? baseH : null, "N2");
            SetDecimal(ws.Cell(row, 8), optH > 0 ? optH : null, "N2");
            SetDecimal(ws.Cell(row, 9), pesH > 0 ? pesH : null, "N2");

            if (d.Level == 0)
            {
                ws.Row(row).Style.Font.Bold = true;
                totalBase += baseH;
                totalOpt += optH;
                totalPes += pesH;
            }
            else if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            row++;
        }

        // Итого
        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Cell(row, 2).Value = "По проекту";
        SetDecimal(ws.Cell(row, 7), totalBase, "N2");
        SetDecimal(ws.Cell(row, 8), totalOpt, "N2");
        SetDecimal(ws.Cell(row, 9), totalPes, "N2");
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        ws.Row(row).Style.Font.FontColor = XLColor.White;

        var tableRange = ws.Range(2, 1, row, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 45;
        ws.Column(3).Width = 10;
        for (var c = 4; c <= headers.Length; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);
    }

    private static void AddPhysicalVolumesSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Физ. объёмы");

        ws.Cell(1, 1).Value = "Физические объёмы по направлениям IIC";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 7).Merge();

        var headers = new[] { "Направление", "Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var d in report.Directions.Where(d => d.PhysicalVolumes.Count > 0))
        {
            ws.Cell(row, 1).Value = $"[{d.Code}] {d.Name}";
            ws.Range(row, 1, row, headers.Length).Merge();
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
            row++;

            foreach (var pv in d.PhysicalVolumes)
            {
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
            if (c != 3) ws.Column(c).AdjustToContents();

        ws.Range(2, 1, Math.Max(3, row - 1), headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, Math.Max(3, row - 1), headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        ws.SheetView.FreezeRows(2);
    }

    private static void AddEquipmentSheet(IXLWorkbook wb, ProjectVersionAnalysisDto report)
    {
        var ws = wb.Worksheets.Add("Оборудование");

        ws.Cell(1, 1).Value = "Оборудование проекта по направлениям IIC";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 7).Merge();

        var headers = new[] { "Направление", "Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var d in report.Directions.Where(d => d.EquipmentVolumes.Count > 0))
        {
            ws.Cell(row, 1).Value = $"[{d.Code}] {d.Name}";
            ws.Range(row, 1, row, headers.Length).Merge();
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
            row++;

            foreach (var eq in d.EquipmentVolumes)
            {
                ws.Cell(row, 2).Value = eq.Code;
                ws.Cell(row, 3).Value = eq.Name;
                ws.Cell(row, 4).Value = eq.UnitName ?? string.Empty;
                ws.Cell(row, 5).Value = eq.Quantity;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00##";
                ws.Cell(row, 6).Value = eq.Coefficient;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.0000";
                ws.Cell(row, 7).Value = eq.Total;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
        }

        ws.Column(1).Width = 4;
        ws.Column(3).Width = 50;
        for (var c = 2; c <= headers.Length; c++)
            if (c != 3) ws.Column(c).AdjustToContents();

        ws.Range(2, 1, Math.Max(3, row - 1), headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, Math.Max(3, row - 1), headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        ws.SheetView.FreezeRows(2);
    }

    private static void BuildDetailExcel(IReadOnlyList<DetailForecastRowDto> rows, string projectCode, string versionName, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Детально");

        ws.Cell(1, 1).Value = $"Детальный прогноз трудозатрат: {projectCode} ({versionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 22).Merge();

        var headers = new[]
        {
            "Шифр объекта", "Наименование объекта",
            "Тип привязки", "Комплект документов", "ID по каталогу", "TAG",
            "ID работы", "Наименование", "Комментарий", "Описательная часть",
            "Код ФО", "Наименование ФО", "Оценка", "Коэффициент",
            "Группа ФО IIC", "Ед. изм.",
            "Производительность", "СО",
            "Кол-во",
            "Базовый, ч", "Оптимист., ч", "Пессимист., ч"
        };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
        string? lastFacility = null;

        foreach (var r in rows)
        {
            var facKey = r.FacilityCode + "|" + r.FacilityName;
            if (facKey != lastFacility)
            {
                var hdr = string.IsNullOrEmpty(r.FacilityCode)
                    ? r.FacilityName
                    : $"{r.FacilityCode} — {r.FacilityName}";
                ws.Cell(row, 1).Value = hdr;
                ws.Range(row, 1, row, headers.Length).Merge();
                ws.Row(row).Style.Font.Bold = true;
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF4FF");
                row++;
                lastFacility = facKey;
            }

            ws.Cell(row, 1).Value = r.FacilityCode;
            ws.Cell(row, 2).Value = r.FacilityName;
            ws.Cell(row, 3).Value = r.BindingType;
            ws.Cell(row, 4).Value = r.DocumentSet;
            ws.Cell(row, 5).Value = r.CatalogId;
            ws.Cell(row, 6).Value = r.Tag;
            ws.Cell(row, 7).Value = r.WorkId;
            ws.Cell(row, 8).Value = r.WorkName;
            ws.Cell(row, 9).Value = r.WorkComment;
            ws.Cell(row, 10).Value = r.EstimationBasis;
            ws.Cell(row, 11).Value = r.PvCode;
            ws.Cell(row, 12).Value = r.PvName;
            ws.Cell(row, 13).Value = r.IsAssessment;
            SetDecimal(ws.Cell(row, 14), r.Coefficient, "N4");
            ws.Cell(row, 15).Value = r.IicGroupName;
            ws.Cell(row, 16).Value = r.IicUnitName;
            SetDecimal(ws.Cell(row, 17), r.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 18), r.NormStdDev, "N4");
            ws.Cell(row, 19).Value = r.Quantity;
            ws.Cell(row, 19).Style.NumberFormat.Format = "#,##0.00##";
            ws.Cell(row, 19).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            SetDecimal(ws.Cell(row, 20), r.BaseHours, "N2");
            SetDecimal(ws.Cell(row, 21), r.OptimisticHours, "N2");
            SetDecimal(ws.Cell(row, 22), r.PessimisticHours, "N2");

            if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            totalBase += r.BaseHours ?? 0m;
            totalOpt  += r.OptimisticHours ?? 0m;
            totalPes  += r.PessimisticHours ?? 0m;
            row++;
        }

        ws.Cell(row, 1).Value = "ИТОГО";
        ws.Range(row, 1, row, 19).Merge();
        SetDecimal(ws.Cell(row, 20), totalBase, "N2");
        SetDecimal(ws.Cell(row, 21), totalOpt, "N2");
        SetDecimal(ws.Cell(row, 22), totalPes, "N2");
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        ws.Row(row).Style.Font.FontColor = XLColor.White;

        ws.Range(2, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(2).Width = 35;
        ws.Column(8).Width = 30;
        ws.Column(12).Width = 30;
        ws.Column(15).Width = 25;
        for (var c = 1; c <= headers.Length; c++)
            if (c != 2 && c != 8 && c != 12 && c != 15) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);

        wb.SaveAs(filePath);
    }

    private static void BuildDetailSummaryTableExcel(DetailForecastSummaryTableDto table, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Сводная таблица");

        var colCount = Math.Max(1, 1 + table.Columns.Count * 2);
        ws.Cell(1, 1).Value = $"Сводная таблица прогноза: {table.ProjectCode} ({table.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, colCount).Merge();

        // Строка 2: заголовки блоков (по 2 столбца)
        ws.Cell(2, 1).Value = "Объект";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

        // Строка 3: подзаголовки ФО / Тр.
        ws.Cell(3, 1).Value = string.Empty;
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

        foreach (var dataRow in table.Rows)
        {
            ws.Cell(rowIdx, 1).Value = dataRow.RowLabel;
            var cellCol = 2;
            for (var i = 0; i < dataRow.Cells.Count; i++)
            {
                var cell = dataRow.Cells[i];
                totalVolumes[i] += cell.PhysicalVolume;
                totalHours[i] += cell.LaborHours;

                if (cell.PhysicalVolume != 0m)
                {
                    ws.Cell(rowIdx, cellCol).Value = cell.PhysicalVolume;
                    ws.Cell(rowIdx, cellCol).Style.NumberFormat.Format = "#,##0.00##";
                    ws.Cell(rowIdx, cellCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                if (cell.LaborHours != 0m)
                {
                    ws.Cell(rowIdx, cellCol + 1).Value = cell.LaborHours;
                    ws.Cell(rowIdx, cellCol + 1).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(rowIdx, cellCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                cellCol += 2;
            }

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
                var vol = Math.Round(totalVolumes[i], 4);
                var hrs = Math.Round(totalHours[i], 2);

                if (vol != 0m)
                {
                    ws.Cell(rowIdx, totalCol).Value = vol;
                    ws.Cell(rowIdx, totalCol).Style.NumberFormat.Format = "#,##0.00##";
                    ws.Cell(rowIdx, totalCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                if (hrs != 0m)
                {
                    ws.Cell(rowIdx, totalCol + 1).Value = hrs;
                    ws.Cell(rowIdx, totalCol + 1).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(rowIdx, totalCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                totalCol += 2;
            }

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
        wb.SaveAs(filePath);
    }

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

    // ─── PDF ─────────────────────────────────────────────────────────────────

    private static void BuildPdf(ProjectVersionAnalysisDto report, string filePath)
    {
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new ForecastWindowsFontResolver();

        var doc = new PdfDocument();
        doc.Info.Title = $"Прогноз трудозатрат: {report.ProjectName}";
        doc.Info.Author = "EngineeringPerformance";

        AddPdfSummaryPage(doc, report);

        foreach (var d in report.Directions.Where(d =>
                     d.PhysicalVolumes.Count > 0 || d.EquipmentVolumes.Count > 0))
            AddPdfDirectionPage(doc, d, report);

        doc.Save(filePath);
    }

    private static void AddPdfSummaryPage(PdfDocument doc, ProjectVersionAnalysisDto report)
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

        double[] colW = [50, 230, 55, 70, 80, 80, 80];
        string[] colH = ["Шифр", "Направление ИИС", "Ед.изм.", "Факт ФО", "Базовый, ч", "Оптим., ч", "Пессим., ч"];

        var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
        DrawPdfRow(gfx, margin, y, colW, colH, fontHeader, XBrushes.White, hdrBg, 14);
        y += 14;

        var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
        var rowIdx = 0;

        foreach (var d in report.Directions)
        {
            if (y + 13 > page.Height.Point - margin) break;
            var (baseH, optH, pesH) = CalcHours(d);
            if (d.ParentId == null) { totalBase += baseH; totalOpt += optH; totalPes += pesH; }

            var indent = new string(' ', Math.Max(0, d.Level) * 2);
            var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
            string[] vals =
            [
                d.Code,
                indent + TruncatePdf(d.Name, 40),
                d.UnitName ?? "—",
                d.FactPhysicalVolume.HasValue ? d.FactPhysicalVolume.Value.ToString("N2") : "—",
                baseH > 0 ? baseH.ToString("N1") : "—",
                optH > 0 ? optH.ToString("N1") : "—",
                pesH > 0 ? pesH.ToString("N1") : "—",
            ];
            DrawPdfRow(gfx, margin, y, colW, vals, fontBody, XBrushes.Black, bg, 12);
            y += 12;
            rowIdx++;
        }

        var totalBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x1E, 0x3A, 0x5F));
        string[] totalVals =
        [
            "ИТОГО", "По проекту", "—", "—",
            totalBase > 0 ? totalBase.ToString("N1") : "—",
            totalOpt > 0 ? totalOpt.ToString("N1") : "—",
            totalPes > 0 ? totalPes.ToString("N1") : "—",
        ];
        DrawPdfRow(gfx, margin, y, colW, totalVals, fontTotal, XBrushes.White, totalBg, 14);
    }

    private static void AddPdfDirectionPage(PdfDocument doc, ProjectVersionDirectionDto d, ProjectVersionAnalysisDto report)
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

        var (baseH, optH, pesH) = CalcHours(d);

        gfx.DrawString($"[{d.Code}] {d.Name}", fontTitle, XBrushes.Black,
            new XRect(margin, y, pageW - margin * 2, 16), XStringFormats.TopLeft);
        y += 18;

        gfx.DrawString(
            $"Факт ФО: {(d.FactPhysicalVolume.HasValue ? d.FactPhysicalVolume.Value.ToString("N2") : "—")}   " +
            $"Норм. произв.: {(d.NormProductivity.HasValue ? d.NormProductivity.Value.ToString("N4") : "—")}   " +
            $"Базовый: {(baseH > 0 ? baseH.ToString("N1") : "—")} ч   " +
            $"Оптимист.: {(optH > 0 ? optH.ToString("N1") : "—")} ч   " +
            $"Пессимист.: {(pesH > 0 ? pesH.ToString("N1") : "—")} ч",
            fontHeader, XBrushes.Black, new XRect(margin, y, pageW - margin * 2, 12), XStringFormats.TopLeft);
        y += 20;

        if (d.PhysicalVolumes.Count > 0)
        {
            gfx.DrawString("Физические объёмы проекта", fontHeader, XBrushes.Black,
                new XRect(margin, y, 300, 12), XStringFormats.TopLeft);
            y += 13;

            double[] pvColW = [50, 300, 55, 60, 55, 70];
            string[] pvHdr = ["Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого"];
            var hdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
            DrawPdfRow(gfx, margin, y, pvColW, pvHdr, fontHeader, XBrushes.White, hdrBg, 12);
            y += 12;

            var bgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
            var rowIdx = 0;
            foreach (var pv in d.PhysicalVolumes)
            {
                if (y + 11 > page.Height.Point - margin) break;
                var bg = rowIdx % 2 == 0 ? XBrushes.White : bgLight;
                string[] vals = [pv.Code, TruncatePdf(pv.Name, 52), pv.UnitName ?? "—",
                    pv.Quantity.ToString("N2"), pv.Coefficient.ToString("N4"), pv.Total.ToString("N2")];
                DrawPdfRow(gfx, margin, y, pvColW, vals, fontBody, XBrushes.Black, bg, 11);
                y += 11;
                rowIdx++;
            }
            y += 8;
        }

        if (d.EquipmentVolumes.Count > 0)
        {
            gfx.DrawString("Оборудование проекта", fontHeader, XBrushes.Black,
                new XRect(margin, y, 300, 12), XStringFormats.TopLeft);
            y += 13;

            double[] eqColW = [50, 300, 55, 60, 55, 70];
            string[] eqHdr = ["Код", "Номенклатурная позиция", "Ед. изм.", "Кол-во", "Коэфф.", "Итого"];
            var eqHdrBg = new XSolidBrush(XColor.FromArgb(0xFF, 0x2C, 0x3E, 0x50));
            DrawPdfRow(gfx, margin, y, eqColW, eqHdr, fontHeader, XBrushes.White, eqHdrBg, 12);
            y += 12;

            var eqBgLight = new XSolidBrush(XColor.FromArgb(0xFF, 0xF8, 0xFA, 0xFC));
            var eqRowIdx = 0;
            foreach (var eq in d.EquipmentVolumes)
            {
                if (y + 11 > page.Height.Point - margin) break;
                var bg = eqRowIdx % 2 == 0 ? XBrushes.White : eqBgLight;
                string[] vals = [eq.Code, TruncatePdf(eq.Name, 52), eq.UnitName ?? "—",
                    eq.Quantity.ToString("N2"), eq.Coefficient.ToString("N4"), eq.Total.ToString("N2")];
                DrawPdfRow(gfx, margin, y, eqColW, vals, fontBody, XBrushes.Black, bg, 11);
                y += 11;
                eqRowIdx++;
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
