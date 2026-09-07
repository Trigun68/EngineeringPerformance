using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
    private readonly IProjectReportSummaryService _reportSummary;
    private readonly IWorkCostService _workCosts;
    private readonly IPerformanceCalculationEngine _engine;
    private readonly AppDbContext _context;

    public VisualReportService(
        IProjectsPageDataFetcher pageData,
        IProjectCompareService compare,
        IProjectVersionAnalysisService analysis,
        ITrendAnalyticsService trends,
        IChartImageRenderer charts,
        IForecastDetailSummaryTableContext meta,
        IPerformanceReportService performanceReport,
        IProjectReportSummaryService reportSummary,
        IWorkCostService workCosts,
        IPerformanceCalculationEngine engine,
        AppDbContext context)
    {
        _pageData = pageData;
        _compare = compare;
        _analysis = analysis;
        _trends = trends;
        _charts = charts;
        _meta = meta;
        _performanceReport = performanceReport;
        _reportSummary = reportSummary;
        _workCosts = workCosts;
        _engine = engine;
        _context = context;
    }

    public async Task<byte[]> ExportForecastSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, createdAt, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
        var totals = data.Forecast.GrandTotals;
        var (contentKpis, coverKpis) = await BuildForecastScenarioKpisAsync(data.Forecast, createdAt, cancellationToken);

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

        var callout = PdfInsightBuilder.BuildForecastCallout(totals.BaseHours, totals.OptimisticHours, totals.PessimisticHours);

        var hierRows = data.Forecast.Summary
            .Where(r => !r.IsTotal)
            .Select(r => new HierarchicalPdfRow(
                r.Level,
                [
                    r.DisplayName,
                    PdfNumberFormat.Volume(r.FactPhysicalVolume),
                    PdfNumberFormat.Hours(r.OptimisticHours),
                    PdfNumberFormat.Hours(r.BaseHours),
                    PdfNumberFormat.Hours(r.PessimisticHours),
                ]));

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, contentKpis));
            if (callout is not null)
                col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutBlock(c, callout));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Сравнение сценариев трудоёмкости", scenarioChart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Top-15 направлений IIC", iicChart, "Длинные подписи усечены"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Итого по направлениям",
                ForecastSummaryColumns,
                hierRows));
        },
        PdfReportKind.Forecast,
        "Прогноз — Итого",
        $"Проект: {projectCode}",
        $"Версия: {versionName}",
        coverKpis);
    }

    public async Task<byte[]> ExportForecastFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, createdAt, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
        var (contentKpis, coverKpis) = await BuildForecastScenarioKpisAsync(data.Forecast, createdAt, cancellationToken);

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
            PdfNumberFormat.Hours(f.OptimisticHours),
            PdfNumberFormat.Hours(f.BaseHours),
            PdfNumberFormat.Hours(f.PessimisticHours),
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, contentKpis));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Базовый сценарий по объектам", chart));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                ForecastFacilityColumns,
                tableRows));
        },
        PdfReportKind.Forecast,
        "Прогноз — По объектам",
        $"Проект: {projectCode}",
        $"Версия: {versionName}",
        coverKpis);
    }

    public Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        ExportFactSummaryPdfAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportFactSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysis.GenerateReportAsync(projectId, versionId, cancellationToken);
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, _, versionName, _, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
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
            new ReportKpiItem("Факт. часы", PdfNumberFormat.HoursWithUnit(summary.TotalFactLaborHours)),
            new ReportKpiItem("Взвеш. Z", PdfNumberFormat.Z(summary.WeightedZScore)),
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
                        PdfNumberFormat.Volume(r.FactPhysicalVolume),
                        PdfNumberFormat.Hours(r.FactLaborHours),
                        ..FactPlanHourCells(r.OptimisticHours, r.BaseHours, r.PessimisticHours),
                        PdfNumberFormat.Z(r.ZScore),
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
                PdfNumberFormat.HoursWithUnit(summary.TotalFactLaborHours),
                PdfNumberFormat.Z(summary.WeightedZScore),
                summary.WeightedZScorePercentile ?? "—",
                interpretationRu));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeZScoreScaleLegend(c, summary.WeightedZScore));
            if (callouts.Count > 0)
                col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutList(c, callouts));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Отклонение Z-score по направлениям", chart, "Top-20 по модулю Z"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Производительность по IIC",
                FactSummaryColumns,
                hierRows,
                interpretationColumnIndex: 8));
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
        var (projectCode, _, versionName, _, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

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
            PdfNumberFormat.Hours(f.LaborHours),
            ..FactPlanHourCells(f.OptimisticHours, f.BaseHours, f.PessimisticHours),
            PdfNumberFormat.Z(f.ZScore),
            PerformancePercentileHelper.GetPercentile(f.ZScore),
            f.Interpretation ?? "—",
        ]);

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Фактические часы по объектам", chart, "Top-15"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                FactFacilityColumns,
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
        await EnsureVersionsBelongToProjectAsync(
            projectId,
            [planVersionId, factVersionId, compareVersionAId, compareVersionBId],
            cancellationToken);

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
            new ReportKpiItem("Прогноз, базовый", PdfNumberFormat.HoursWithUnit(planData.Forecast.GrandTotals.BaseHours)),
            new ReportKpiItem("Факт. часы", PdfNumberFormat.HoursWithUnit(analysis.Summary.TotalFactLaborHours)),
            new ReportKpiItem("Взвеш. Z", PdfNumberFormat.Z(analysis.Summary.WeightedZScore)),
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
                    CombinedAppendixColumns,
                    planData.Forecast.Summary.Where(r => !r.IsTotal)
                        .Select(r => new HierarchicalPdfRow(r.Level, [r.DisplayName, PdfNumberFormat.Hours(r.BaseHours)]))));
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
            new("Δ часы (объекты)", PdfNumberFormat.HoursWithUnit(totalHoursDelta)),
            new("Средн. Δ Z факт", avgZDelta is null ? "—" : PdfNumberFormat.Z((decimal)avgZDelta.Value)),
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
                PdfNumberFormat.Volume(r.FactPhysicalVolumeA),
                PdfNumberFormat.Volume(r.FactPhysicalVolumeB),
                ..ComparePlanHourCells(r.PlannedLaborHoursOptimisticA, r.PlannedLaborHoursA, r.PlannedLaborHoursPessimisticA,
                    r.PlannedLaborHoursOptimisticB, r.PlannedLaborHoursB, r.PlannedLaborHoursPessimisticB),
                PdfNumberFormat.Hours(r.FactLaborHoursA),
                PdfNumberFormat.Hours(r.FactLaborHoursB),
                PdfNumberFormat.Productivity(r.PlannedProductivityA),
                PdfNumberFormat.Productivity(r.PlannedProductivityB),
                PdfNumberFormat.Productivity(r.FactProductivityA),
                PdfNumberFormat.Productivity(r.FactProductivityB),
                PdfNumberFormat.Z(r.FactZScoreA),
                PdfNumberFormat.Z(r.FactZScoreB),
            ]);

        col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
        if (callout is not null)
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeCalloutBlock(c, callout));
        col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, $"Изменение {chart.Metric} между версиями", chartPng));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Итого IIC",
            CompareSummaryColumns,
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
            PdfNumberFormat.Volume(f.FactPhysicalVolumeA),
            PdfNumberFormat.Volume(f.FactPhysicalVolumeB),
            ..ComparePlanHourCells(f.PlannedLaborHoursOptimisticA, f.PlannedLaborHoursA, f.PlannedLaborHoursPessimisticA,
                f.PlannedLaborHoursOptimisticB, f.PlannedLaborHoursB, f.PlannedLaborHoursPessimisticB),
            PdfNumberFormat.Hours(f.FactLaborHoursA),
            PdfNumberFormat.Hours(f.FactLaborHoursB),
            PdfNumberFormat.Productivity(f.PlannedProductivityA),
            PdfNumberFormat.Productivity(f.PlannedProductivityB),
            PdfNumberFormat.Productivity(f.FactProductivityA),
            PdfNumberFormat.Productivity(f.FactProductivityB),
            PdfNumberFormat.Z(f.ZScoreA),
            PdfNumberFormat.Z(f.ZScoreB),
        ]);
        var tableRows = facility.Count == 0
            ? dataRows
            : dataRows.Append(
            [
                "ИТОГО",
                SumNullableDisplay(facility.Select(f => f.FactPhysicalVolumeA), PdfNumberFormat.VolumeDecimals),
                SumNullableDisplay(facility.Select(f => f.FactPhysicalVolumeB), PdfNumberFormat.VolumeDecimals),
                ..ComparePlanHourCells(
                    SumNullable(facility.Select(f => f.PlannedLaborHoursOptimisticA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursPessimisticA)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursOptimisticB)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursB)),
                    SumNullable(facility.Select(f => f.PlannedLaborHoursPessimisticB))),
                SumNullableDisplay(facility.Select(f => f.FactLaborHoursA), PdfNumberFormat.HoursDecimals),
                SumNullableDisplay(facility.Select(f => f.FactLaborHoursB), PdfNumberFormat.HoursDecimals),
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
            CompareFacilityColumns,
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
            PdfNumberFormat.Volume(d.QuantityA),
            PdfNumberFormat.Volume(d.QuantityB),
            ..ComparePlanHourCells(d.PlannedLaborHoursOptimisticA, d.PlannedLaborHoursA, d.PlannedLaborHoursPessimisticA,
                d.PlannedLaborHoursOptimisticB, d.PlannedLaborHoursB, d.PlannedLaborHoursPessimisticB),
            PdfNumberFormat.Hours(d.FactLaborHoursA),
            PdfNumberFormat.Hours(d.FactLaborHoursB),
            PdfNumberFormat.Productivity(d.PlannedProductivityA),
            PdfNumberFormat.Productivity(d.PlannedProductivityB),
            PdfNumberFormat.Productivity(d.FactProductivityA),
            PdfNumberFormat.Productivity(d.FactProductivityB),
            PdfNumberFormat.Z(d.ZScoreA),
            PdfNumberFormat.Z(d.ZScoreB),
            d.Presence,
        ]);

        col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, "Наибольшие отклонения по позициям", chartPng));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Детально",
            CompareDetailColumns,
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

        static string FmtZ(decimal? z) => PdfNumberFormat.SignedZ(z);
        static string FmtHours(decimal? h) => PdfNumberFormat.HoursWithUnit(h);

        var kpis = new List<ReportKpiItem>
        {
            new("Заложенный Z", FmtZ(report.Plan?.WeightedZScore)),
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
            .Select(d =>
            {
                var volume = PerformanceDisplayVolume.ForMergedRow(d);
                return (string[])
                [
                    d.Name,
                    volume is null ? "—" : $"{PdfNumberFormat.Volume(volume)} {d.UnitName ?? ""}".Trim(),
                    PdfNumberFormat.Hours(d.PlanLaborHours),
                    PdfNumberFormat.Hours(d.FactLaborHours),
                    PdfNumberFormat.Productivity(d.PlanProductivity),
                    PdfNumberFormat.Productivity(d.FactProductivity),
                    PdfNumberFormat.Productivity(d.NormProductivity),
                    PdfNumberFormat.Productivity(d.NormStdDev),
                    FmtZ(d.PlanZScore),
                    FmtZ(d.FactZScore),
                ];
            })
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
                    PerformanceDirectionsColumns,
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

    /// <summary>
    /// Таблицы групп IIC — первый уровень (часы факта после rollup на ParentId == null).
    /// Сводка — из <see cref="IProjectReportSummaryService"/> (семантика PerformanceReportService).
    /// </summary>
    public async Task<byte[]> ExportProjectReportPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var reportSummary = await _reportSummary.GetSummaryAsync(projectId, cancellationToken);
        var report = await _analysis.GenerateReportAsync(projectId, versionId, cancellationToken);
        var (projectCode, _, versionName, _, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var allRows = data.Fact.Performance.Where(r => r.WorkDirectionId >= 0).ToList();
        // Таблицы отчёта — IIC первого уровня: часы факта после RollHoursToFirstLevel живут на корнях.
        var firstLevel = PerformanceDisplayVolume.FirstLevel(allRows).ToList();

        var scoredRoots = firstLevel.Where(r => r.ZScore.HasValue).ToList();
        var best = scoredRoots.OrderBy(r => r.ZScore!.Value).Take(5).ToList();
        var worst = scoredRoots.OrderByDescending(r => r.ZScore!.Value).Take(5).ToList();

        static IEnumerable<string[]> RankRows(IReadOnlyList<PerformanceRowDto> rows) =>
            rows.Select((r, i) => new[]
            {
                (i + 1).ToString(),
                r.WorkDirectionName,
                PdfNumberFormat.Hours(r.SubtreeHours ?? r.FactLaborHours),
                PdfNumberFormat.Z(r.ZScore),
                r.Interpretation?.ToRussian() ?? "—",
            });

        var volumeRows = firstLevel.Select(r => new HierarchicalPdfRow(
            0,
            [
                r.WorkDirectionName,
                PdfNumberFormat.Volume(PerformanceDisplayVolume.ForReportRow(r)),
                PdfNumberFormat.Hours(r.FactLaborHours ?? r.SubtreeHours),
                PdfNumberFormat.Productivity(r.FactProductivity),
                PdfNumberFormat.Productivity(r.NormProductivity),
                PdfNumberFormat.Z(r.ZScore),
                r.Interpretation?.ToRussian() ?? "—",
            ],
            InterpretationText: r.Interpretation?.ToRussian()));

        var planColumn = new PdfPlanFactColumn(
            reportSummary.PlanVersionName ?? "Основная версия не назначена",
            PdfNumberFormat.Hours(reportSummary.PlanHours),
            PdfNumberFormat.Z(reportSummary.PlanWeightedZScore),
            reportSummary.PlanPercentile ?? "Н/Д",
            reportSummary.PlanInterpretation ?? "—");
        var factColumn = reportSummary.FactVersionId.HasValue
            ? new PdfPlanFactColumn(
                reportSummary.FactVersionName ?? "—",
                PdfNumberFormat.Hours(reportSummary.FactHours),
                PdfNumberFormat.Z(reportSummary.FactWeightedZScore),
                reportSummary.FactPercentile ?? "Н/Д",
                reportSummary.FactInterpretation ?? "—")
            : null;
        var deltaCaption = reportSummary.FactHours.HasValue && reportSummary.PlanHours.HasValue
            ? $"Δ трудоёмкость: {PdfNumberFormat.Hours(reportSummary.FactHours.Value - reportSummary.PlanHours.Value)} ч относительно плана"
            : null;

        var headlineInterpretation = reportSummary.FactInterpretation ?? reportSummary.PlanInterpretation;
        var teaserKpis = new[]
        {
            new ReportKpiItem("Часы (факт/план)", PdfNumberFormat.HoursWithUnit(reportSummary.FactHours ?? reportSummary.PlanHours)),
            new ReportKpiItem("Взвеш. Z", PdfNumberFormat.Z(reportSummary.FactWeightedZScore ?? reportSummary.PlanWeightedZScore)),
            new ReportKpiItem("Оценка", headlineInterpretation ?? "—"),
        };

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposePlanFactSummary(c, planColumn, factColumn, deltaCaption));
            col.Item().PaddingTop(10).Element(c => QuestPdfReportComposer.ComposeZScoreScaleLegend(
                c, (double?)reportSummary.PlanWeightedZScore, (double?)reportSummary.FactWeightedZScore));

            col.Item().PaddingTop(10).Text("Топ-5 направлений IIC").SemiBold()
                .FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
            col.Item().Row(row =>
            {
                row.Spacing(10);
                row.RelativeItem().Element(c => QuestPdfReportComposer.ComposeTable(
                    c, "Лучшие (наименьший Z — опережение нормы)", RankColumns, RankRows(best), interpretationColumnIndex: 4));
                row.RelativeItem().Element(c => QuestPdfReportComposer.ComposeTable(
                    c, "Худшие (наибольший Z — перерасход)", RankColumns, RankRows(worst), interpretationColumnIndex: 4));
            });

            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Все группы физобъёмов IIC",
                VolumeGroupColumns,
                volumeRows,
                interpretationColumnIndex: 6));
        },
        PdfReportKind.Report,
        "Отчёт по проекту — план/факт",
        $"Проект: {report.ProjectName}",
        $"Факт. версия: {report.VersionName}",
        teaserKpis);
    }

    /// <summary>
    /// Контент — только часы; титул — часы и стоимость (ставка as-of CreatedAt × собственные часы направления).
    /// </summary>
    private async Task<(ReportKpiItem[] Content, ReportKpiItem[] Cover)> BuildForecastScenarioKpisAsync(
        ProjectsPageForecastGridsDto forecast,
        DateTime versionCreatedAt,
        CancellationToken cancellationToken)
    {
        var totals = forecast.GrandTotals;
        var content = PdfScenarioKpis.HoursOnly(totals.OptimisticHours, totals.BaseHours, totals.PessimisticHours);
        var costs = await ComputeScenarioWorkCostsAsync(forecast.Summary, versionCreatedAt, cancellationToken);
        var cover = PdfScenarioKpis.HoursWithCost(
            totals.OptimisticHours, totals.BaseHours, totals.PessimisticHours, costs);
        return (content, cover);
    }

    private async Task<ScenarioWorkCostResult?> ComputeScenarioWorkCostsAsync(
        IReadOnlyList<ProjectsPageForecastSummaryRowDto> summary,
        DateTime versionCreatedAt,
        CancellationToken cancellationToken)
    {
        var rows = await _workCosts.GetAsOfAsync(versionCreatedAt, cancellationToken);
        var rates = rows
            .Where(r => r.RatePerHour is not null)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First().RatePerHour!.Value);

        var samples = summary
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0)
            .Select(r => new WorkCostHourSample(
                r.WorkDirectionId, r.OptimisticHours, r.BaseHours, r.PessimisticHours));

        return _engine.ComputeScenarioWorkCosts(samples, rates);
    }

    private static readonly PdfTableColumn[] ForecastSummaryColumns =
    [
        PdfTableColumn.Text("Направление", 2.4f),
        PdfTableColumn.Number("План. объём"),
        PdfTableColumn.Optimistic(),
        PdfTableColumn.BaseHours(),
        PdfTableColumn.Pessimistic(),
    ];

    private static readonly PdfTableColumn[] ForecastFacilityColumns =
    [
        PdfTableColumn.Text("Код"),
        PdfTableColumn.Text("Объект", 2.2f),
        PdfTableColumn.Optimistic(),
        PdfTableColumn.BaseHours(),
        PdfTableColumn.Pessimistic(),
    ];

    private static readonly PdfTableColumn[] FactSummaryColumns =
    [
        PdfTableColumn.Text("Направление", 2.4f),
        PdfTableColumn.Number("ФО"),
        PdfTableColumn.Number("Часы"),
        PdfTableColumn.Optimistic(),
        PdfTableColumn.BaseHours(),
        PdfTableColumn.Pessimistic(),
        PdfTableColumn.Number("Z"),
        PdfTableColumn.Text("Перцентиль"),
        PdfTableColumn.Text("Оценка"),
    ];

    private static readonly PdfTableColumn[] FactFacilityColumns =
    [
        PdfTableColumn.Text("Код"),
        PdfTableColumn.Text("Объект", 2.2f),
        PdfTableColumn.Number("Часы"),
        PdfTableColumn.Optimistic(),
        PdfTableColumn.BaseHours(),
        PdfTableColumn.Pessimistic(),
        PdfTableColumn.Number("Z"),
        PdfTableColumn.Text("Перцентиль"),
        PdfTableColumn.Text("Оценка"),
    ];

    private static readonly PdfTableColumn[] CombinedAppendixColumns =
    [
        PdfTableColumn.Text("Направление", 2.4f),
        PdfTableColumn.BaseHours(),
    ];

    private static readonly PdfTableColumn[] RankColumns =
    [
        PdfTableColumn.Text("#", 0.4f),
        PdfTableColumn.Text("Направление IIC", 2.4f),
        PdfTableColumn.Number("Часы"),
        PdfTableColumn.Number("Z"),
        PdfTableColumn.Text("Оценка"),
    ];

    private static readonly PdfTableColumn[] VolumeGroupColumns =
    [
        PdfTableColumn.Text("Направление IIC", 2.6f),
        PdfTableColumn.Number("Факт. объём"),
        PdfTableColumn.Number("Часы"),
        PdfTableColumn.Number("Произв. (факт)"),
        PdfTableColumn.Number("Норма IIC"),
        PdfTableColumn.Number("Z"),
        PdfTableColumn.Text("Оценка"),
    ];

    private static readonly PdfTableColumn[] PerformanceDirectionsColumns =
    [
        PdfTableColumn.Text("Направление", 2.2f),
        PdfTableColumn.Number("Объём"),
        PdfTableColumn.Number("Труд. план"),
        PdfTableColumn.Number("Труд. факт"),
        PdfTableColumn.Number("Произв. план"),
        PdfTableColumn.Number("Произв. факт"),
        PdfTableColumn.Number("Норма"),
        PdfTableColumn.Number("σ"),
        PdfTableColumn.Number("Заложенный Z"),
        PdfTableColumn.Number("Z факт"),
    ];

    private static readonly PdfTableColumn[] ComparePlanHourColumns =
    [
        PdfTableColumn.Optimistic(PdfScenarioLabels.OptimisticA, 1.3f),
        PdfTableColumn.BaseHours(PdfScenarioLabels.BaseA, 1.2f),
        PdfTableColumn.Pessimistic(PdfScenarioLabels.PessimisticA, 1.3f),
        PdfTableColumn.Optimistic(PdfScenarioLabels.OptimisticB, 1.3f),
        PdfTableColumn.BaseHours(PdfScenarioLabels.BaseB, 1.2f),
        PdfTableColumn.Pessimistic(PdfScenarioLabels.PessimisticB, 1.3f),
    ];

    private static readonly PdfTableColumn[] CompareSummaryColumns =
    [
        PdfTableColumn.Text("Направление", 2.2f),
        PdfTableColumn.Number("ФО A"),
        PdfTableColumn.Number("ФО B"),
        ..ComparePlanHourColumns,
        PdfTableColumn.Number("Факт. труд A"),
        PdfTableColumn.Number("Факт. труд B"),
        PdfTableColumn.Number("План. произв. A"),
        PdfTableColumn.Number("План. произв. B"),
        PdfTableColumn.Number("Факт. произв. A"),
        PdfTableColumn.Number("Факт. произв. B"),
        PdfTableColumn.Number("Z A"),
        PdfTableColumn.Number("Z B"),
    ];

    private static readonly PdfTableColumn[] CompareFacilityColumns =
    [
        PdfTableColumn.Text("Объект", 2.2f),
        PdfTableColumn.Number("ФО A"),
        PdfTableColumn.Number("ФО B"),
        ..ComparePlanHourColumns,
        PdfTableColumn.Number("Факт. труд A"),
        PdfTableColumn.Number("Факт. труд B"),
        PdfTableColumn.Number("План. произв. A"),
        PdfTableColumn.Number("План. произв. B"),
        PdfTableColumn.Number("Факт. произв. A"),
        PdfTableColumn.Number("Факт. произв. B"),
        PdfTableColumn.Number("Z A"),
        PdfTableColumn.Number("Z B"),
    ];

    private static readonly PdfTableColumn[] CompareDetailColumns =
    [
        PdfTableColumn.Text("Объект", 1.6f),
        PdfTableColumn.Text("Код"),
        PdfTableColumn.Text("Наименование", 1.8f),
        PdfTableColumn.Number("ФО A"),
        PdfTableColumn.Number("ФО B"),
        ..ComparePlanHourColumns,
        PdfTableColumn.Number("Факт. труд A"),
        PdfTableColumn.Number("Факт. труд B"),
        PdfTableColumn.Number("План. произв. A"),
        PdfTableColumn.Number("План. произв. B"),
        PdfTableColumn.Number("Факт. произв. A"),
        PdfTableColumn.Number("Факт. произв. B"),
        PdfTableColumn.Number("Z A"),
        PdfTableColumn.Number("Z B"),
        PdfTableColumn.Text("Наличие"),
    ];

    private static string[] FactPlanHourCells(decimal? opt, decimal? baseHours, decimal? pes) =>
    [
        PdfNumberFormat.Hours(opt),
        PdfNumberFormat.Hours(baseHours),
        PdfNumberFormat.Hours(pes),
    ];

    private static string[] ComparePlanHourCells(
        decimal? optA, decimal? baseA, decimal? pesA,
        decimal? optB, decimal? baseB, decimal? pesB) =>
    [
        PdfNumberFormat.Hours(optA),
        PdfNumberFormat.Hours(baseA),
        PdfNumberFormat.Hours(pesA),
        PdfNumberFormat.Hours(optB),
        PdfNumberFormat.Hours(baseB),
        PdfNumberFormat.Hours(pesB),
    ];

    private static decimal? SumNullable(IEnumerable<decimal?> values)
    {
        var sum = values.Where(v => v.HasValue).Select(v => v!.Value).Sum();
        return sum > 0m ? Math.Round(sum, 2) : null;
    }

    private static string SumNullableDisplay(IEnumerable<decimal?> values, int decimals) =>
        PdfNumberFormat.Format(SumNullable(values), decimals);

    private async Task EnsureVersionsBelongToProjectAsync(
        int projectId,
        IReadOnlyList<int> versionIds,
        CancellationToken cancellationToken)
    {
        var versions = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => versionIds.Contains(v.Id))
            .Select(v => new { v.Id, v.ProjectId })
            .ToListAsync(cancellationToken);

        foreach (var vid in versionIds)
        {
            var found = versions.FirstOrDefault(v => v.Id == vid);
            if (found is null)
                throw new InvalidOperationException($"Версия с идентификатором {vid} не найдена.");
            if (found.ProjectId != projectId)
                throw new InvalidOperationException($"Версия {vid} принадлежит другому проекту.");
        }
    }
}
