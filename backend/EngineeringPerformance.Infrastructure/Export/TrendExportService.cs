using System.Globalization;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Excel/PDF блоков экрана «Тренд» — тот же визуальный слой, что сводный отчёт.</summary>
public sealed class TrendExportService(
    IProjectTrendService projectTrend,
    IZTrendAnalyticsService zTrends) : ITrendExportService
{
    public async Task<byte[]> ExportProjectExcelAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var data = await projectTrend.GetAsync(projectId, cancellationToken);
        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        WriteProjectSheets(workbook, data);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportProjectPdfAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var data = await projectTrend.GetAsync(projectId, cancellationToken);
        var project = $"{data.ProjectCode} — {data.ProjectName}";
        var version = data.FactVersionName ?? data.PlanVersionName ?? string.Empty;
        return QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeProjectPdf(col, data),
            PdfReportKind.Trend,
            "Тренд",
            project,
            version);
    }

    public async Task<byte[]> ExportAnalyticsExcelAsync(ZTrendRequest request, CancellationToken cancellationToken = default)
    {
        var data = await zTrends.GetZTrendsAsync(request, cancellationToken);
        using var culture = new TemporaryCulture(CultureInfo.GetCultureInfo("ru-RU"));
        using var workbook = new XLWorkbook();
        WriteAnalyticsSheets(workbook, data);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportAnalyticsPdfAsync(ZTrendRequest request, CancellationToken cancellationToken = default)
    {
        var data = await zTrends.GetZTrendsAsync(request, cancellationToken);
        return QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeAnalyticsPdf(col, data),
            PdfReportKind.Trend,
            "Тренд оценки",
            "Портфель",
            string.Empty);
    }

    private static void WriteProjectSheets(XLWorkbook workbook, ProjectTrendDto data)
    {
        var overview = workbook.Worksheets.Add("Обзор");
        WriteHeader(overview, 1, ["Показатель", "Значение"]);
        var r = 2;
        overview.Cell(r, 1).Value = "Проект";
        overview.Cell(r++, 2).Value = $"{data.ProjectCode} — {data.ProjectName}";
        overview.Cell(r, 1).Value = "Z известен, доля часов";
        overview.Cell(r++, 2).Value = Pct(data.Coverage.ShareWithZ);
        overview.Cell(r, 1).Value = "Пара план/факт, доля часов";
        overview.Cell(r++, 2).Value = Pct(data.Coverage.ShareWithBridgePair);
        overview.Cell(r, 1).Value = "Сырой Δ часов";
        overview.Cell(r++, 2).Value = Dec(data.RawHoursDelta);
        overview.Columns().AdjustToContents();

        var zSheet = workbook.Worksheets.Add("Z по датам");
        WriteHeader(zSheet, 1, ["Дата", "Направление", "Версия", "Z", "Часы"]);
        r = 2;
        foreach (var p in data.ZHistory)
        {
            zSheet.Cell(r, 1).Value = p.Date;
            zSheet.Cell(r, 2).Value = p.Direction;
            zSheet.Cell(r, 3).Value = p.VersionName;
            zSheet.Cell(r, 4).Value = Dec(p.ZScore);
            zSheet.Cell(r, 5).Value = p.Hours;
            r++;
        }

        zSheet.Columns().AdjustToContents();

        var bridge = workbook.Worksheets.Add("Мост часов");
        WriteHeader(bridge, 1, ["Группа IIC", "Эффект объёма, ч", "Эффект производительности, ч"]);
        r = 2;
        foreach (var line in data.HoursBridge.Lines)
        {
            bridge.Cell(r, 1).Value = line.GroupName;
            bridge.Cell(r, 2).Value = line.VolumeEffect;
            bridge.Cell(r, 3).Value = line.ProductivityEffect;
            r++;
        }

        bridge.Columns().AdjustToContents();

        var zero = workbook.Worksheets.Add("Z от 0");
        WriteHeader(zero, 1, ["Группа", "Z"]);
        r = 2;
        foreach (var bar in data.ZFromZero)
        {
            zero.Cell(r, 1).Value = bar.Name;
            zero.Cell(r, 2).Value = bar.ZScore;
            r++;
        }

        zero.Columns().AdjustToContents();

        var scurve = workbook.Worksheets.Add("S-curve");
        WriteHeader(scurve, 1, ["Дата", "Накопительный факт, ч", "Накопительный план, ч"]);
        r = 2;
        var dates = data.SCurve.Select(p => p.Date).Union(data.PlanSCurve.Select(p => p.Date)).OrderBy(d => d).ToList();
        var factMap = data.SCurve.GroupBy(p => p.Date).ToDictionary(g => g.Key, g => g.Last().CumulativeHours);
        var planMap = data.PlanSCurve.GroupBy(p => p.Date).ToDictionary(g => g.Key, g => g.Last().CumulativeHours);
        foreach (var date in dates)
        {
            scurve.Cell(r, 1).Value = date;
            if (factMap.TryGetValue(date, out var fh))
                scurve.Cell(r, 2).Value = fh;
            if (planMap.TryGetValue(date, out var ph))
                scurve.Cell(r, 3).Value = ph;
            r++;
        }

        scurve.Columns().AdjustToContents();
        WriteWaterfallSheet(workbook, "Водопад факта", data.DeltaWaterfall);
        WriteWaterfallSheet(workbook, "Водопад плана", data.PlanDeltaWaterfall);
    }

    private static void WriteAnalyticsSheets(XLWorkbook workbook, ZTrendResponse data)
    {
        var overview = workbook.Worksheets.Add("Обзор");
        WriteHeader(overview, 1, ["Показатель", "Значение"]);
        var coverage = data.Coverage ?? HoursCoverageDto.Empty;
        var r = 2;
        overview.Cell(r, 1).Value = "Z известен, доля часов";
        overview.Cell(r++, 2).Value = Pct(coverage.ShareWithZ);
        overview.Cell(r, 1).Value = "Пара план/факт, доля часов";
        overview.Cell(r++, 2).Value = Pct(coverage.ShareWithBridgePair);
        overview.Cell(r, 1).Value = "Сырой Δ часов";
        overview.Cell(r++, 2).Value = Dec(data.RawHoursDelta);
        overview.Columns().AdjustToContents();

        var movers = workbook.Worksheets.Add("Кто тянет");
        WriteHeader(movers, 1, ["Код", "Проект", "Z", "Вес часов", "Вклад"]);
        r = 2;
        foreach (var m in data.Movers ?? [])
        {
            movers.Cell(r, 1).Value = m.ProjectCode;
            movers.Cell(r, 2).Value = m.ProjectName;
            movers.Cell(r, 3).Value = Dec(m.ZScore);
            movers.Cell(r, 4).Value = m.Weight;
            movers.Cell(r, 5).Value = m.Contribution;
            r++;
        }

        movers.Columns().AdjustToContents();

        var bridge = workbook.Worksheets.Add("Мост часов");
        WriteHeader(bridge, 1, ["Группа IIC", "Эффект объёма, ч", "Эффект производительности, ч"]);
        r = 2;
        foreach (var line in data.DisciplineBridge?.Lines ?? [])
        {
            bridge.Cell(r, 1).Value = line.GroupName;
            bridge.Cell(r, 2).Value = line.VolumeEffect;
            bridge.Cell(r, 3).Value = line.ProductivityEffect;
            r++;
        }

        bridge.Columns().AdjustToContents();
        WriteWaterfallSheet(workbook, "Водопад факта", data.Detail?.FactWaterfall);
        WriteWaterfallSheet(workbook, "Водопад плана", data.Detail?.PlanWaterfall);
    }

    private static void WriteWaterfallSheet(XLWorkbook workbook, string name, IReadOnlyList<WaterfallStepDto>? steps)
    {
        var sheet = workbook.Worksheets.Add(name);
        WriteHeader(sheet, 1, ["Фактор", "Вклад"]);
        var r = 2;
        foreach (var step in steps ?? [])
        {
            sheet.Cell(r, 1).Value = step.Name;
            sheet.Cell(r, 2).Value = step.Value;
            r++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void ComposeProjectPdf(ColumnDescriptor col, ProjectTrendDto data)
    {
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Покрытие часов",
            ["Показатель", "Значение"],
            [
                ["Z известен", Pct(data.Coverage.ShareWithZ)],
                ["Пара план/факт", Pct(data.Coverage.ShareWithBridgePair)],
                ["Сырой Δ часов", Dec(data.RawHoursDelta)],
            ]));
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Мост часов",
            ["Группа IIC", "Объём, ч", "Производительность, ч"],
            data.HoursBridge.Lines.Select(l => new[] { l.GroupName, Dec(l.VolumeEffect), Dec(l.ProductivityEffect) })));
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Z от 0",
            ["Группа", "Z"],
            data.ZFromZero.Select(b => new[] { b.Name, Dec(b.ZScore) })));
        if (data.PlanDeltaWaterfall is { Count: > 0 })
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c,
                "Водопад ΔZ плана",
                ["Фактор", "Вклад"],
                data.PlanDeltaWaterfall.Select(s => new[] { s.Name, Dec(s.Value) })));
        }
    }

    private static void ComposeAnalyticsPdf(ColumnDescriptor col, ZTrendResponse data)
    {
        var coverage = data.Coverage ?? HoursCoverageDto.Empty;
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Покрытие часов",
            ["Показатель", "Значение"],
            [
                ["Z известен", Pct(coverage.ShareWithZ)],
                ["Пара план/факт", Pct(coverage.ShareWithBridgePair)],
                ["Сырой Δ часов", Dec(data.RawHoursDelta)],
            ]));
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Кто тянет",
            ["Код", "Проект", "Z", "Вклад"],
            (data.Movers ?? []).Select(m => new[] { m.ProjectCode, m.ProjectName, Dec(m.ZScore), Dec(m.Contribution) })));
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c,
            "Мост часов",
            ["Группа IIC", "Объём, ч", "Производительность, ч"],
            (data.DisciplineBridge?.Lines ?? []).Select(l => new[] { l.GroupName, Dec(l.VolumeEffect), Dec(l.ProductivityEffect) })));
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, string[] headers)
    {
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static string Pct(decimal? share) =>
        share is null ? "—" : $"{Math.Round(share.Value * 100m, 1, MidpointRounding.ToEven)} %";

    private static string Dec(decimal? value) =>
        value is null ? "—" : value.Value.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
}
