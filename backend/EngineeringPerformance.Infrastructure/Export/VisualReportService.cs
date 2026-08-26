using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>PDF-отчёты с графиками ScottPlot + QuestPDF.</summary>
public sealed class VisualReportService : IVisualReportService
{
    private readonly IProjectsPageDataFetcher _pageData;
    private readonly IProjectCompareService _compare;
    private readonly IProjectVersionAnalysisService _analysis;
    private readonly ITrendAnalyticsService _trends;
    private readonly IChartImageRenderer _charts;
    private readonly IForecastDetailSummaryTableContext _meta;
    private readonly IPerformanceReportService _performanceReport;

    public VisualReportService(
        IProjectsPageDataFetcher pageData,
        IProjectCompareService compare,
        IProjectVersionAnalysisService analysis,
        ITrendAnalyticsService trends,
        IChartImageRenderer charts,
        IForecastDetailSummaryTableContext meta,
        IPerformanceReportService performanceReport)
    {
        _pageData = pageData;
        _compare = compare;
        _analysis = analysis;
        _trends = trends;
        _charts = charts;
        _meta = meta;
        _performanceReport = performanceReport;
    }

    public async Task<byte[]> ExportForecastSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
        var totals = data.Forecast.GrandTotals;

        var scenarioChart = _charts.RenderScenarioTotalsChart(
            "Сценарии трудоёмкости (итого)",
            (double)totals.BaseHours,
            (double)totals.OptimisticHours,
            (double)totals.PessimisticHours);

        var topIic = data.Forecast.Summary
            .Where(r => !r.IsTotal && r.BaseHours > 0)
            .OrderByDescending(r => r.BaseHours)
            .Take(15)
            .Select(r => new ChartBarItem(r.DisplayName, (double)(r.BaseHours ?? 0), ReportVisualTheme.AccentA))
            .ToList();
        var iicChart = _charts.RenderHorizontalBarChart(
            "Top-15 IIC по базовым часам",
            topIic,
            options: new ChartRenderOptions(
                YAxisLabel: "Направление IIC",
                XAxisLabel: "Часы",
                MaxLabelLength: ReportVisualTheme.DefaultMaxLabelLength));

        var kpis = new[]
        {
            new ReportKpiItem("Базовый", $"{totals.BaseHours:N0} ч"),
            new ReportKpiItem("Оптимист.", $"{totals.OptimisticHours:N0} ч"),
            new ReportKpiItem("Пессимист.", $"{totals.PessimisticHours:N0} ч"),
        };

        var callout = PdfInsightBuilder.BuildForecastCallout(totals.BaseHours, totals.OptimisticHours, totals.PessimisticHours);

        var hierRows = data.Forecast.Summary
            .Where(r => !r.IsTotal)
            .Select(r => new HierarchicalPdfRow(
                r.Level,
                [
                    r.DisplayName,
                    r.FactPhysicalVolume?.ToString("N2") ?? "—",
                    r.OptimisticHours?.ToString("N0") ?? "—",
                    r.BaseHours?.ToString("N0") ?? "—",
                    r.PessimisticHours?.ToString("N0") ?? "—",
                ]));

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            if (callout is not null)
                col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutBlock(c, callout));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Сравнение сценариев трудоёмкости", scenarioChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Top-15 направлений IIC", iicChart, "Длинные подписи усечены"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Итого по направлениям",
                ["Направление", "План. объём", "Опт.", "База", "Пессим."],
                hierRows));
        },
        PdfReportKind.Forecast,
        "Прогноз — Итого",
        $"Проект: {projectCode}",
        $"Версия: {versionName}",
        kpis);
    }

    public async Task<byte[]> ExportForecastFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var chartItems = data.Forecast.Facility
            .OrderByDescending(f => f.BaseHours ?? 0)
            .Take(15)
            .Select(f => new ChartBarItem(f.FacilityName, (double)(f.BaseHours ?? 0), ReportVisualTheme.AccentA))
            .ToList();
        var chart = _charts.RenderHorizontalBarChart(
            "Часы по объектам (базовый сценарий)",
            chartItems,
            options: new ChartRenderOptions(YAxisLabel: "Объект", XAxisLabel: "Часы"));

        var tableRows = data.Forecast.Facility.Select(f => new[]
        {
            f.FacilityCode,
            f.FacilityName,
            f.OptimisticHours?.ToString("N0") ?? "—",
            f.BaseHours?.ToString("N0") ?? "—",
            f.PessimisticHours?.ToString("N0") ?? "—",
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Базовый сценарий по объектам", chart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                ["Код", "Объект", "Опт.", "База ч", "Пессим."],
                tableRows));
        },
        PdfReportKind.Forecast,
        "Прогноз — По объектам",
        $"Проект: {projectCode}",
        $"Версия: {versionName}");
    }

    public Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        ExportFactSummaryPdfAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportFactSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysis.GenerateReportAsync(projectId, versionId, cancellationToken);
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
        var summary = report.Summary;

        var chartItems = data.Fact.Performance
            .Where(r => r.WorkDirectionId >= 0 && r.ZScore.HasValue)
            .OrderByDescending(r => Math.Abs(r.ZScore!.Value))
            .Take(20)
            .Select(r => new ChartBarItem(
                r.WorkDirectionName,
                (double)r.ZScore!.Value,
                ReportVisualTheme.InterpretationColor(r.Interpretation?.ToRussian())))
            .ToList();
        var chart = _charts.RenderHorizontalBarChart(
            "Z-score по направлениям IIC",
            chartItems,
            options: new ChartRenderOptions(
                YAxisLabel: "Направление IIC",
                XAxisLabel: "Z-score",
                MaxLabelLength: ReportVisualTheme.DefaultMaxLabelLength));

        var interpretationRu = summary.WeightedZScoreInterpretation?.ToRussian() ?? "—";

        var teaserKpis = new[]
        {
            new ReportKpiItem("Факт. часы", summary.TotalFactLaborHours?.ToString("N0") ?? "—"),
            new ReportKpiItem("Взвеш. Z", summary.WeightedZScore?.ToString("N2") ?? "—"),
            new ReportKpiItem("Оценка", interpretationRu),
        };
        var callouts = PdfInsightBuilder.BuildFactCallouts(data.Fact.Performance, summary);

        var hierRows = data.Fact.Performance
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r =>
            {
                var interp = r.Interpretation?.ToRussian() ?? "—";
                return new HierarchicalPdfRow(
                    r.Level,
                    [
                        r.WorkDirectionName,
                        r.WorkDirectionCode,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.FactLaborHours?.ToString("N0") ?? "—",
                        ..FactPlanHourCells(r.OptimisticHours, r.BaseHours, r.PessimisticHours),
                        r.ZScore?.ToString("N2") ?? "—",
                        PerformancePercentileHelper.GetPercentile(r.ZScore),
                        interp,
                    ],
                    InterpretationText: interp);
            });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeProjectAssessmentKpis(
                c,
                "Факт. часы",
                summary.TotalFactLaborHours?.ToString("N0") ?? "—",
                summary.WeightedZScore?.ToString("N2") ?? "—",
                summary.WeightedZScorePercentile ?? "—",
                interpretationRu));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeZScoreScaleLegend(c, summary.WeightedZScore));
            if (callouts.Count > 0)
                col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutList(c, callouts));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Отклонение Z-score по направлениям", chart, "Top-20 по модулю Z"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Производительность по IIC",
                ["Направление", "Код", "ФО", "Часы", ..FactPlanHourHeaders, "Z", "Перцентиль", "Оценка"],
                hierRows,
                interpretationColumnIndex: 9));
        },
        PdfReportKind.Fact,
        "Оценка факта — Итого",
        $"Проект: {report.ProjectName ?? projectCode}",
        $"Версия: {report.VersionName ?? versionName}",
        teaserKpis);
    }

    public async Task<byte[]> ExportFactFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var chartItems = data.Fact.Facility
            .OrderByDescending(f => f.LaborHours ?? 0)
            .Take(15)
            .Select(f => new ChartBarItem(f.FacilityName, (double)(f.LaborHours ?? 0), ReportVisualTheme.AccentA))
            .ToList();
        var chart = _charts.RenderVerticalBarChart(
            "Факт. часы по объектам",
            chartItems,
            options: new ChartRenderOptions(XAxisLabel: "Объект", YAxisLabel: "Часы"));

        var tableRows = data.Fact.Facility.Select(f => (string[])
        [
            f.FacilityCode,
            f.FacilityName,
            f.LaborHours?.ToString("N0") ?? "—",
            ..FactPlanHourCells(f.OptimisticHours, f.BaseHours, f.PessimisticHours),
            f.ZScore?.ToString("N2") ?? "—",
            PerformancePercentileHelper.GetPercentile(f.ZScore),
            f.Interpretation ?? "—",
        ]);

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Фактическая трудоёмкость по объектам", chart, "Top-15"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                ["Код", "Объект", "Часы", ..FactPlanHourHeaders, "Z", "Перцентиль", "Оценка"],
                tableRows,
                interpretationColumnIndex: 8));
        },
        PdfReportKind.Fact,
        "Оценка факта — По объектам",
        $"Проект: {projectCode}",
        $"Версия: {versionName}");
    }

    public async Task<byte[]> ExportComparePdfAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CompareReportSection section = CompareReportSection.Full,
        bool showOnlyDifferences = false,
        int top = 20,
        CancellationToken cancellationToken = default)
    {
        var compare = await _compare.CompareAsync(projectId, versionAId, versionBId, cancellationToken);
        var chart = await _compare.GetChartAsync(projectId, versionAId, versionBId, "iic", "zScore", top, cancellationToken);

        if (section == CompareReportSection.Full)
        {
            return QuestPdfReportComposer.BuildLandscapeReport(col =>
            {
                ComposeCompareSummary(col, compare, chart, showOnlyDifferences, top);
                col.Item().PageBreak();
                ComposeCompareFacility(col, compare, chart, showOnlyDifferences, top);
                col.Item().PageBreak();
                ComposeCompareDetail(col, compare, showOnlyDifferences, top);
            },
            PdfReportKind.Compare,
            "Сравнение версий — Полный отчёт",
            $"A: {chart.VersionAName}",
            $"B: {chart.VersionBName}");
        }

        if (section is CompareReportSection.Summary)
            return BuildCompareSummaryPdf(compare, chart, showOnlyDifferences, top);
        if (section == CompareReportSection.Facility)
            return BuildCompareFacilityPdf(compare, chart, showOnlyDifferences, top);
        return BuildCompareDetailPdf(compare, showOnlyDifferences, top);
    }

    public async Task<byte[]> ExportTrendsPdfAsync(
        int projectId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string formulaId = "v1-docset-proportional",
        CancellationToken cancellationToken = default)
    {
        var trends = await _trends.GetTrendsAsync(projectId, dateFrom, dateTo, "version", formulaId, cancellationToken);

        static IReadOnlyList<(DateTime X, double Y)> ToPoints(TrendSeriesDto? series) =>
            series?.Points.Select(p => (p.Timestamp, (double)(p.Value ?? 0))).ToList()
            ?? [];

        var factSeries = trends.Series.FirstOrDefault(s => s.Metric == "factLaborHours");
        var forecastSeries = trends.Series.FirstOrDefault(s => s.Metric == "forecastLaborHours");
        var deltaSeries = trends.Series.FirstOrDefault(s => s.Metric == "hoursDelta");
        var cumulativeSeries = trends.Series.FirstOrDefault(s => s.Metric == "cumulativeFactHours");

        var lineChart = _charts.RenderMultiLineChart(
            "Факт vs Прогноз",
            new[]
            {
                new ChartLineSeries("Факт", ToPoints(factSeries), ReportVisualTheme.AccentA),
                new ChartLineSeries("Прогноз/План", ToPoints(forecastSeries), ReportVisualTheme.AccentB),
            },
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight,
            new ChartRenderOptions(XAxisLabel: "Дата", YAxisLabel: "Часы"));

        var deltaItems = deltaSeries?.Points
            .Select(p => new ChartBarItem(p.Label ?? p.Timestamp.ToString("dd.MM.yy"), (double)(p.Value ?? 0)))
            .ToList() ?? [];
        var columnChart = _charts.RenderDeltaBarChart(
            "Δ Факт − Прогноз",
            deltaItems,
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight,
            new ChartRenderOptions(XAxisLabel: "Версия", YAxisLabel: "Δ часы"));

        var areaChart = cumulativeSeries is not null
            ? _charts.RenderAreaChart(
                "S-curve — накопительный факт",
                new ChartAreaSeries("Накопительный факт", ToPoints(cumulativeSeries), ReportVisualTheme.AccentA),
                ReportVisualTheme.ChartWidth,
                ReportVisualTheme.ChartHeight,
                new ChartRenderOptions(XAxisLabel: "Дата", YAxisLabel: "Накопительно, ч"))
            : Array.Empty<byte>();

        var deltaPoints = (deltaSeries?.Points ?? []).Select(p => (p.Label, p.Timestamp, p.Value)).ToList();
        var callout = PdfInsightBuilder.BuildTrendCallout(deltaPoints);

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            if (callout is not null)
                col.Item().PaddingBottom(8).Element(c => QuestPdfReportComposer.ComposeCalloutBlock(c, callout));
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Динамика фактических и плановых часов", lineChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Отклонение факта от прогноза по версиям", columnChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Кривая накопления фактических часов", areaChart));
        },
        PdfReportKind.Trend,
        "Трендовый анализ",
        $"Проект ID: {projectId}",
        $"Формула: {trends.Formula.DisplayName}",
        null,
        $"Гранулярность: {trends.Granularity}");
    }

    public async Task<byte[]> ExportCombinedPdfAsync(
        int projectId,
        int planVersionId,
        int factVersionId,
        int compareVersionAId,
        int compareVersionBId,
        string formulaId = "v1-docset-proportional",
        bool includeAppendix = false,
        CancellationToken cancellationToken = default)
    {
        var planData = await _pageData.GetProjectsPageDataAsync(projectId, planVersionId, null, cancellationToken);
        var factData = await _pageData.GetProjectsPageDataAsync(projectId, factVersionId, null, cancellationToken);
        var analysis = await _analysis.GenerateReportAsync(projectId, factVersionId, cancellationToken);
        var compareChart = await _compare.GetChartAsync(projectId, compareVersionAId, compareVersionBId, "iic", "zScore", 10, cancellationToken);
        var trends = await _trends.GetTrendsAsync(projectId, null, null, "version", formulaId, cancellationToken);

        var forecastChart = _charts.RenderScenarioTotalsChart(
            "Прогноз — сценарии",
            (double)planData.Forecast.GrandTotals.BaseHours,
            (double)planData.Forecast.GrandTotals.OptimisticHours,
            (double)planData.Forecast.GrandTotals.PessimisticHours,
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight);

        var factChartItems = factData.Fact.Performance
            .Where(r => r.WorkDirectionId >= 0 && r.ZScore.HasValue)
            .OrderByDescending(r => Math.Abs(r.ZScore!.Value))
            .Take(10)
            .Select(r => new ChartBarItem(r.WorkDirectionName, (double)r.ZScore!.Value, ReportVisualTheme.InterpretationColor(r.Interpretation?.ToRussian())))
            .ToList();
        var factChart = _charts.RenderHorizontalBarChart(
            "Факт — Z top-10",
            factChartItems,
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight,
            new ChartRenderOptions(YAxisLabel: "Направление IIC", XAxisLabel: "Z-score"));

        var compareDeltaItems = compareChart.Points
            .Select(p => new ChartBarItem(p.Category, (double)(p.Delta ?? 0)))
            .ToList();
        var compareChartPng = _charts.RenderDeltaBarChart(
            "Сравнение — Δ Z",
            compareDeltaItems,
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight,
            new ChartRenderOptions(XAxisLabel: "Направление IIC", YAxisLabel: "Δ Z"));

        var factSeries = trends.Series.FirstOrDefault(s => s.Metric == "factLaborHours");
        var forecastSeries = trends.Series.FirstOrDefault(s => s.Metric == "forecastLaborHours");
        var trendChart = _charts.RenderMultiLineChart(
            "Тренд",
            new[]
            {
                new ChartLineSeries("Факт", factSeries?.Points.Select(p => (p.Timestamp, (double)(p.Value ?? 0))).ToList() ?? [], ReportVisualTheme.AccentA),
                new ChartLineSeries("Прогноз", forecastSeries?.Points.Select(p => (p.Timestamp, (double)(p.Value ?? 0))).ToList() ?? [], ReportVisualTheme.AccentB),
            },
            ReportVisualTheme.ChartWidth,
            ReportVisualTheme.ChartHeight,
            new ChartRenderOptions(XAxisLabel: "Дата", YAxisLabel: "Часы"));

        var kpis = new[]
        {
            new ReportKpiItem("Прогноз база", $"{planData.Forecast.GrandTotals.BaseHours:N0} ч"),
            new ReportKpiItem("Факт часы", analysis.Summary.TotalFactLaborHours?.ToString("N0") ?? "—"),
            new ReportKpiItem("Взвеш. Z", analysis.Summary.WeightedZScore?.ToString("N2") ?? "—"),
            new ReportKpiItem("Оценка", analysis.Summary.WeightedZScoreInterpretation?.ToRussian() ?? "—"),
        };

        var callouts = PdfInsightBuilder.BuildFactCallouts(factData.Fact.Performance, analysis.Summary);

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeZScoreScaleLegend(c, analysis.Summary.WeightedZScore));
            if (callouts.Count > 0)
                col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutList(c, callouts));
            col.Item().PaddingTop(10).Text("Ключевые визуализации").SemiBold().FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
            col.Item().PaddingTop(6).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "1. Прогнозирование — сценарии трудоёмкости", forecastChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "2. Оценка факта — Z-score по направлениям", factChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, $"3. Сравнение {compareChart.VersionAName} / {compareChart.VersionBName}", compareChartPng));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "4. Тренды — факт vs прогноз", trendChart));
            if (includeAppendix)
            {
                col.Item().PageBreak();
                col.Item().Text("Приложение: прогноз по направлениям").SemiBold().FontColor(ReportVisualTheme.Primary);
                col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                    c, "",
                    ["Направление", "База ч"],
                    planData.Forecast.Summary.Where(r => !r.IsTotal)
                        .Select(r => new HierarchicalPdfRow(r.Level, [r.DisplayName, r.BaseHours?.ToString("N0") ?? "—"]))));
            }
        },
        PdfReportKind.Combined,
        "Сводный отчёт",
        $"Проект ID: {projectId}",
        $"План v{planVersionId} · Факт v{factVersionId} · Сравнение {compareVersionAId}/{compareVersionBId}",
        kpis);
    }

    private byte[] BuildCompareSummaryPdf(
        VersionCompareResultDto compare,
        CompareChartResultDto chart,
        bool showOnlyDifferences,
        int top) =>
        QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeCompareSummary(col, compare, chart, showOnlyDifferences, top),
            PdfReportKind.Compare,
            "Сравнение версий — Итого",
            $"A: {chart.VersionAName}",
            $"B: {chart.VersionBName}");

    private void ComposeCompareSummary(
        ColumnDescriptor col,
        VersionCompareResultDto compare,
        CompareChartResultDto chart,
        bool showOnlyDifferences,
        int top)
    {
        var summary = CompareReportFilter.FilterSummary(compare.Summary, showOnlyDifferences);
        var totalHoursDelta = compare.Facility.Sum(f => f.HoursDelta ?? 0);
        var zRows = summary.Where(r => r.FactZDelta.HasValue).ToList();
        var avgZDelta = zRows.Count > 0 ? zRows.Average(r => (double)(r.FactZDelta ?? 0)) : (double?)null;

        var deltaItems = chart.Points
            .Select(p => new ChartBarItem(p.Category, (double)(p.Delta ?? 0)))
            .ToList();
        var chartPng = _charts.RenderDeltaBarChart(
            $"Δ {chart.Metric} ({chart.Level})",
            deltaItems,
            options: new ChartRenderOptions(XAxisLabel: "Направление IIC", YAxisLabel: $"Δ {chart.Metric}"));

        var kpis = new List<ReportKpiItem>
        {
            new("Δ часы (объекты)", $"{totalHoursDelta:N2} ч"),
            new("Средн. Δ Z факт", avgZDelta?.ToString("N4") ?? "—"),
            new("Версия A", chart.VersionAName),
            new("Версия B", chart.VersionBName),
        };

        var callout = PdfInsightBuilder.BuildCompareCallout(
            summary.Select(r => (r.Name, r.FactZDelta ?? r.PlanZDelta)),
            chart.VersionAName,
            chart.VersionBName);

        var tableRows = summary
            .Where(r => r.FactPhysicalVolumeA.HasValue || r.FactPhysicalVolumeB.HasValue
                || r.PlannedLaborHoursA.HasValue || r.PlannedLaborHoursB.HasValue
                || r.PlannedLaborHoursOptimisticA.HasValue || r.PlannedLaborHoursOptimisticB.HasValue
                || r.PlannedLaborHoursPessimisticA.HasValue || r.PlannedLaborHoursPessimisticB.HasValue
                || r.FactLaborHoursA.HasValue || r.FactLaborHoursB.HasValue
                || r.FactZScoreA.HasValue || r.FactZScoreB.HasValue)
            .OrderByDescending(r => Math.Abs(r.FactZDelta ?? r.PlanZDelta ?? 0))
            .Select(r => (string[])
            [
                r.Name,
                r.FactPhysicalVolumeA?.ToString("N2") ?? "—",
                r.FactPhysicalVolumeB?.ToString("N2") ?? "—",
                ..ComparePlanHourCells(r.PlannedLaborHoursOptimisticA, r.PlannedLaborHoursA, r.PlannedLaborHoursPessimisticA,
                    r.PlannedLaborHoursOptimisticB, r.PlannedLaborHoursB, r.PlannedLaborHoursPessimisticB),
                r.FactLaborHoursA?.ToString("N0") ?? "—",
                r.FactLaborHoursB?.ToString("N0") ?? "—",
                r.PlannedProductivityA?.ToString("N4") ?? "—",
                r.PlannedProductivityB?.ToString("N4") ?? "—",
                r.FactProductivityA?.ToString("N4") ?? "—",
                r.FactProductivityB?.ToString("N4") ?? "—",
                r.FactZScoreA?.ToString("N4") ?? "—",
                r.FactZScoreB?.ToString("N4") ?? "—",
            ]);

        col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
        if (callout is not null)
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutBlock(c, callout));
        col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, $"Изменение {chart.Metric} между версиями", chartPng));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Итого IIC",
            ["Направление", "ФО A", "ФО B", ..ComparePlanHourHeaders, "Факт. труд A", "Факт. труд B",
                "План. произв. A", "План. произв. B", "Факт. произв. A", "Факт. произв. B", "Z A", "Z B"],
            tableRows));
    }

    private byte[] BuildCompareFacilityPdf(
        VersionCompareResultDto compare,
        CompareChartResultDto chart,
        bool showOnlyDifferences,
        int top) =>
        QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeCompareFacility(col, compare, chart, showOnlyDifferences, top),
            PdfReportKind.Compare,
            "Сравнение версий — По объектам",
            $"A: {chart.VersionAName}",
            $"B: {chart.VersionBName}");

    private void ComposeCompareFacility(
        ColumnDescriptor col,
        VersionCompareResultDto compare,
        CompareChartResultDto chart,
        bool showOnlyDifferences,
        int top)
    {
        var facility = CompareReportFilter.FilterFacility(compare.Facility, showOnlyDifferences);
        var grouped = facility
            .OrderByDescending(f => Math.Abs(f.HoursDelta ?? 0))
            .Take(top)
            .Select(f => new ChartGroupedBarItem(
                f.FacilityName,
                (double)(f.FactLaborHoursA ?? 0),
                (double)(f.FactLaborHoursB ?? 0)))
            .ToList();
        var chartPng = _charts.RenderGroupedBarChart(
            "Часы A vs B по объектам",
            grouped,
            chart.VersionAName,
            chart.VersionBName,
            options: new ChartRenderOptions(XAxisLabel: "Объект", YAxisLabel: "Часы"));

        var dataRows = facility.Select(f => (string[])
        [
            f.FacilityName,
            f.FactPhysicalVolumeA?.ToString("N2") ?? "—",
            f.FactPhysicalVolumeB?.ToString("N2") ?? "—",
            ..ComparePlanHourCells(f.PlannedLaborHoursOptimisticA, f.PlannedLaborHoursA, f.PlannedLaborHoursPessimisticA,
                f.PlannedLaborHoursOptimisticB, f.PlannedLaborHoursB, f.PlannedLaborHoursPessimisticB),
            f.FactLaborHoursA?.ToString("N0") ?? "—",
            f.FactLaborHoursB?.ToString("N0") ?? "—",
            f.PlannedProductivityA?.ToString("N4") ?? "—",
            f.PlannedProductivityB?.ToString("N4") ?? "—",
            f.FactProductivityA?.ToString("N4") ?? "—",
            f.FactProductivityB?.ToString("N4") ?? "—",
            f.ZScoreA?.ToString("N4") ?? "—",
            f.ZScoreB?.ToString("N4") ?? "—",
        ]);
        var tableRows = facility.Count == 0
            ? dataRows
            : dataRows.Append(
            [
                "ИТОГО",
                SumNullableDisplay(facility.Select(f => f.FactPhysicalVolumeA), "N2"),
                SumNullableDisplay(facility.Select(f => f.FactPhysicalVolumeB), "N2"),
                ..ComparePlanHourCells(
                    SumNullable(facility.Select(f => f.PlannedLaborHoursOptimisticA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursPessimisticA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursOptimisticB)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursB)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursPessimisticB))),
                SumNullableDisplay(facility.Select(f => f.FactLaborHoursA), "N0"),
                SumNullableDisplay(facility.Select(f => f.FactLaborHoursB), "N0"),
                "—",
                "—",
                "—",
                "—",
                "—",
                "—",
            ]);

        col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Сравнение трудоёмкости по объектам", chartPng));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "По объектам",
            ["Объект", "ФО A", "ФО B", ..ComparePlanHourHeaders, "Факт. труд A", "Факт. труд B",
                "План. произв. A", "План. произв. B", "Факт. произв. A", "Факт. произв. B", "Z A", "Z B"],
            tableRows));
    }

    private byte[] BuildCompareDetailPdf(
        VersionCompareResultDto compare,
        bool showOnlyDifferences,
        int top) =>
        QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeCompareDetail(col, compare, showOnlyDifferences, top),
            PdfReportKind.Compare,
            "Сравнение версий — Детально",
            "Постатейное сравнение",
            $"Позиций: {compare.Detail.Count}");

    private void ComposeCompareDetail(
        ColumnDescriptor col,
        VersionCompareResultDto compare,
        bool showOnlyDifferences,
        int top)
    {
        var detail = CompareReportFilter.FilterDetail(compare.Detail, showOnlyDifferences);
        var chartItems = detail
            .OrderByDescending(d => Math.Abs(d.HoursDelta ?? 0))
            .Take(top)
            .Select(d => new ChartBarItem($"{d.PvCode}", (double)Math.Abs(d.HoursDelta ?? 0), ReportVisualTheme.DeltaColor(d.HoursDelta ?? 0)))
            .ToList();
        var chartPng = _charts.RenderHorizontalBarChart(
            "Top |Δ часов|",
            chartItems,
            options: new ChartRenderOptions(YAxisLabel: "Код позиции", XAxisLabel: "|Δ часы|"));

        var tableRows = detail.Select(d => (string[])
        [
            d.FacilityName,
            d.PvCode,
            d.PvName,
            d.QuantityA?.ToString("N2") ?? "—",
            d.QuantityB?.ToString("N2") ?? "—",
            ..ComparePlanHourCells(d.PlannedLaborHoursOptimisticA, d.PlannedLaborHoursA, d.PlannedLaborHoursPessimisticA,
                d.PlannedLaborHoursOptimisticB, d.PlannedLaborHoursB, d.PlannedLaborHoursPessimisticB),
            d.FactLaborHoursA?.ToString("N0") ?? "—",
            d.FactLaborHoursB?.ToString("N0") ?? "—",
            d.PlannedProductivityA?.ToString("N4") ?? "—",
            d.PlannedProductivityB?.ToString("N4") ?? "—",
            d.FactProductivityA?.ToString("N4") ?? "—",
            d.FactProductivityB?.ToString("N4") ?? "—",
            d.ZScoreA?.ToString("N4") ?? "—",
            d.ZScoreB?.ToString("N4") ?? "—",
            d.Presence,
        ]);

        col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Наибольшие отклонения по позициям", chartPng));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Детально",
            ["Объект", "Код", "Наименование", "ФО A", "ФО B", ..ComparePlanHourHeaders,
                "Факт. труд A", "Факт. труд B", "План. произв. A", "План. произв. B",
                "Факт. произв. A", "Факт. произв. B", "Z A", "Z B", "Наличие"],
            tableRows));
    }

    public async Task<byte[]> ExportPerformanceReportPdfAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _performanceReport.GetReportAsync(
            projectId,
            planVersionId,
            factVersionId,
            cancellationToken);

        static string FmtZ(decimal? z) => z is null ? "—" : z.Value.ToString("+0.00;-0.00;0.00");
        static string FmtHours(decimal? h) => h is null ? "—" : $"{h.Value:N0} ч";
        static string FmtNum(decimal? v, int digits = 2) =>
            v is null ? "—" : v.Value.ToString($"N{digits}");

        var kpis = new List<ReportKpiItem>
        {
            new("Z-оценка план", FmtZ(report.Plan?.WeightedZScore)),
            new("Z-оценка факт", FmtZ(report.Fact?.WeightedZScore)),
            new("Трудоёмкость план", FmtHours(report.Plan?.TotalLaborHours)),
            new("Трудоёмкость факт", FmtHours(report.Fact?.TotalLaborHours)),
            new("Точность сравнения", report.Accuracy.AveragePercent is null
                ? "—"
                : $"{report.Accuracy.AveragePercent:0.0}%"),
        };

        var scaleMeta =
            $"Перцентиль плана: {report.Plan?.WeightedZScorePercentile ?? "—"}; " +
            $"перцентиль факта: {report.Fact?.WeightedZScorePercentile ?? "—"}; " +
            $"источник норм: {report.NormSourcesUsed}";

        var treeRows = report.Directions
            .Take(40)
            .Select(d => (string[])
            [
                d.Name,
                d.PhysicalVolume is null ? "—" : $"{FmtNum(d.PhysicalVolume)} {d.UnitName ?? ""}".Trim(),
                FmtNum(d.PlanLaborHours, 0),
                FmtNum(d.FactLaborHours, 0),
                FmtNum(d.PlanProductivity, 4),
                FmtNum(d.FactProductivity, 4),
                FmtNum(d.NormProductivity, 4),
                FmtNum(d.NormStdDev, 4),
                FmtZ(d.PlanZScore),
                FmtZ(d.FactZScore),
            ])
            .ToList();

        var insightLines = report.Insights.Items
            .Take(8)
            .Select(i => $"{i.Title}: {i.Detail}")
            .ToList();

        var historyLines = report.VersionHistory
            .Where(h => h.WeightedZScore.HasValue)
            .Select(h =>
                $"{h.CreatedAt:yyyy-MM-dd} · {(h.Direction == Domain.Enums.AnalysisDirection.Plan ? "План" : "Факт")} · {h.VersionName}: Z={FmtZ(h.WeightedZScore)}")
            .Take(20)
            .ToList();

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            col.Item().PaddingTop(6).Text(scaleMeta).FontSize(8);

            if (historyLines.Count > 0)
            {
                col.Item().PaddingTop(10).Text("Динамика Z-оценки по версиям").SemiBold().FontSize(11);
                foreach (var line in historyLines)
                    col.Item().PaddingTop(1).Text($"• {line}").FontSize(8);
            }

            if (insightLines.Count > 0)
            {
                col.Item().PaddingTop(10).Text("Выводы и рекомендации").SemiBold().FontSize(11);
                foreach (var line in insightLines)
                    col.Item().PaddingTop(2).Text($"• {line}").FontSize(8);
            }

            if (treeRows.Count > 0)
            {
                col.Item().PaddingTop(10).Element(c => QuestPdfReportComposer.ComposeTable(
                    c,
                    "Направление",
                    [
                        "Направление", "Объём", "Труд. план", "Труд. факт",
                        "Произв. план", "Произв. факт", "Норма", "σ", "Z план", "Z факт",
                    ],
                    treeRows));
            }
        },
        PdfReportKind.Combined,
        "Отчет по инженерной производительности",
        $"Проект: {report.Project.Code} — {report.Project.Name}",
        $"{(report.Plan is null ? "План: —" : $"План: {report.Plan.VersionName}")} · {(report.Fact is null ? "Факт: —" : $"Факт: {report.Fact.VersionName}")}",
        kpis,
        $"ГИП: {report.Project.Gip ?? "—"}; заказчик: {report.Project.Customer ?? "—"}; статус: {report.Project.WorkStatus ?? "—"}",
        $"План версий: {report.Project.PlanVersionCount}; факт версий: {report.Project.FactVersionCount}");
    }

    private static readonly string[] FactPlanHourHeaders =
    [
        "План.Труд.Оптим.", "План.Труд.Баз.", "План.Труд.Песс.",
    ];

    private static string[] FactPlanHourCells(decimal? opt, decimal? baseHours, decimal? pes) =>
    [
        opt?.ToString("N2") ?? "—",
        baseHours?.ToString("N2") ?? "—",
        pes?.ToString("N2") ?? "—",
    ];

    private static readonly string[] ComparePlanHourHeaders =
    [
        "План.Труд. (Опт. v. А)", "План.Труд. (Баз. v. А)", "План.Труд. (Песс. v. А)",
        "План.Труд. (Опт. v. Б)", "План.Труд. (Баз. v. Б)", "План.Труд. (Песс. v. Б)",
    ];

    private static string[] ComparePlanHourCells(
        decimal? optA, decimal? baseA, decimal? pesA,
        decimal? optB, decimal? baseB, decimal? pesB) =>
    [
        optA?.ToString("N2") ?? "—",
        baseA?.ToString("N2") ?? "—",
        pesA?.ToString("N2") ?? "—",
        optB?.ToString("N2") ?? "—",
        baseB?.ToString("N2") ?? "—",
        pesB?.ToString("N2") ?? "—",
    ];

    private static decimal? SumNullable(IEnumerable<decimal?> values)
    {
        var sum = values.Where(v => v.HasValue).Select(v => v!.Value).Sum();
        return sum > 0m ? Math.Round(sum, 2) : null;
    }

    private static string SumNullableDisplay(IEnumerable<decimal?> values, string format)
    {
        var total = SumNullable(values);
        return total?.ToString(format) ?? "—";
    }
}
