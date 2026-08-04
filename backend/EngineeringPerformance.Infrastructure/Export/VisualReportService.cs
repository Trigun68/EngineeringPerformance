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
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
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

        var hierRows = data.Forecast.Summary
            .Where(r => !r.IsTotal)
            .Select(r => new HierarchicalPdfRow(
                r.Level,
                [
                    r.DisplayName,
                    r.FactPhysicalVolume?.ToString("N2") ?? "—",
                    r.BaseHours?.ToString("N0") ?? "—",
                    r.OptimisticHours?.ToString("N0") ?? "—",
                    r.PessimisticHours?.ToString("N0") ?? "—",
                ]));

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, scenarioChart, "Сравнение сценариев трудоёмкости"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, iicChart, "Top-15 направлений IIC (длинные подписи усечены)"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Итого по направлениям",
                ["Направление", "План. объём", "База", "Опт.", "Пессим."],
                hierRows));
        }, "Прогноз — Итого", $"Проект: {projectCode}", $"Версия: {versionName}");
    }

    public async Task<byte[]> ExportForecastFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

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
            f.BaseHours?.ToString("N0") ?? "—",
            f.OptimisticHours?.ToString("N0") ?? "—",
            f.PessimisticHours?.ToString("N0") ?? "—",
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chart, "Базовый сценарий по объектам"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                ["Код", "Объект", "База ч", "Опт.", "Пессим."],
                tableRows));
        }, "Прогноз — По объектам", $"Проект: {projectCode}", $"Версия: {versionName}");
    }

    public async Task<byte[]> ExportForecastDetailPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var chartItems = data.Forecast.Detail
            .OrderByDescending(d => d.BaseHours ?? 0)
            .Take(20)
            .Select(d => new ChartBarItem($"{d.CatalogCode}", (double)(d.BaseHours ?? 0), ReportVisualTheme.AccentA))
            .ToList();
        var chart = _charts.RenderHorizontalBarChart(
            "Top-20 позиций по базовым часам",
            chartItems,
            options: new ChartRenderOptions(YAxisLabel: "Код позиции", XAxisLabel: "Часы"));

        var tableRows = data.Forecast.Detail.Select(d => new[]
        {
            d.FacilityName,
            d.CatalogCode,
            d.CatalogName,
            d.Quantity.ToString("N2"),
            d.BaseHours?.ToString("N0") ?? "—",
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chart, "Топ позиций по трудоёмкости"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "Детализация",
                ["Объект", "Код", "Наименование", "Кол-во", "База ч"],
                tableRows));
        }, "Прогноз — Детально", $"Проект: {projectCode}", $"Версия: {versionName}");
    }

    public Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default) =>
        ExportFactSummaryPdfAsync(projectId, versionId, cancellationToken);

    public async Task<byte[]> ExportFactSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var report = await _analysis.GenerateReportAsync(projectId, versionId);
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);
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
            col.Item().PaddingTop(8).Element(QuestPdfReportComposer.ComposeZScoreScaleLegend);
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, chart, "Отклонение Z-score по направлениям (Top-20)"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                c, "Производительность по IIC",
                ["Направление", "Код", "ФО", "Часы", "Z", "Перцентиль", "Оценка"],
                hierRows,
                interpretationColumnIndex: 6));
        }, "Оценка факта — Итого", $"Проект: {report.ProjectName ?? projectCode}", $"Версия: {report.VersionName ?? versionName}");
    }

    public async Task<byte[]> ExportFactFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var chartItems = data.Fact.Facility
            .OrderByDescending(f => f.LaborHours ?? 0)
            .Take(15)
            .Select(f => new ChartBarItem(f.FacilityName, (double)(f.LaborHours ?? 0), ReportVisualTheme.AccentA))
            .ToList();
        var chart = _charts.RenderVerticalBarChart(
            "Факт. часы по объектам",
            chartItems,
            options: new ChartRenderOptions(XAxisLabel: "Объект", YAxisLabel: "Часы"));

        var tableRows = data.Fact.Facility.Select(f => new[]
        {
            f.FacilityCode,
            f.FacilityName,
            f.LaborHours?.ToString("N0") ?? "—",
            f.ZScore?.ToString("N2") ?? "—",
            PerformancePercentileHelper.GetPercentile(f.ZScore),
            f.Interpretation ?? "—",
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chart, "Фактическая трудоёмкость по объектам (Top-15)"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "По объектам",
                ["Код", "Объект", "Часы", "Z", "Перцентиль", "Оценка"],
                tableRows,
                interpretationColumnIndex: 5));
        }, "Оценка факта — По объектам", $"Проект: {projectCode}", $"Версия: {versionName}");
    }

    public async Task<byte[]> ExportFactDetailPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var data = await _pageData.GetProjectsPageDataAsync(projectId, versionId, null, cancellationToken);
        var (projectCode, versionName, _) = await _meta.LoadAsync(projectId, versionId, cancellationToken);

        var chartItems = data.Fact.Detail
            .Where(d => d.ZScore.HasValue)
            .OrderByDescending(d => Math.Abs(d.ZScore!.Value))
            .Take(20)
            .Select(d => new ChartBarItem(
                $"{d.CatalogCode}",
                (double)Math.Abs(d.ZScore!.Value),
                ReportVisualTheme.InterpretationColor(d.Interpretation)))
            .ToList();
        var chart = _charts.RenderHorizontalBarChart(
            "Top-20 |Z| по позициям",
            chartItems,
            options: new ChartRenderOptions(YAxisLabel: "Код позиции", XAxisLabel: "|Z|"));

        var tableRows = data.Fact.Detail.Select(d => new[]
        {
            d.FacilityName,
            d.CatalogCode,
            d.CatalogName,
            d.Quantity.ToString("N2"),
            d.FactLaborHours?.ToString("N0") ?? "—",
            d.ZScore?.ToString("N2") ?? "—",
            PerformancePercentileHelper.GetPercentile(d.ZScore),
            d.Interpretation ?? "—",
        });

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chart, "Абсолютное отклонение Z по позициям (Top-20)"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
                c, "Детализация",
                ["Объект", "Код", "Наименование", "Кол-во", "Часы", "Z", "Перцентиль", "Оценка"],
                tableRows,
                maxRows: 200,
                interpretationColumnIndex: 7));
        }, "Оценка факта — Детально", $"Проект: {projectCode}", $"Версия: {versionName}");
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
            }, "Сравнение версий — Полный отчёт", $"A: {chart.VersionAName}", $"B: {chart.VersionBName}");
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

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, lineChart, "Динамика фактических и плановых часов"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, columnChart, "Отклонение факта от прогноза по версиям"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, areaChart, "Кривая накопления фактических часов"));
        },
        "Трендовый анализ",
        $"Проект ID: {projectId}",
        $"Формула: {trends.Formula.DisplayName}",
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
        var analysis = await _analysis.GenerateReportAsync(projectId, factVersionId);
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

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Text("Сводный отчёт по проекту").SemiBold().FontSize(16).FontColor(ReportVisualTheme.Primary);
            col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            col.Item().PaddingTop(8).Element(QuestPdfReportComposer.ComposeZScoreScaleLegend);
            col.Item().PaddingTop(10).Text("Ключевые визуализации").SemiBold().FontSize(12);
            col.Item().PaddingTop(6).Element(c => QuestPdfReportComposer.ComposeChartImage(c, forecastChart, "1. Прогнозирование — сценарии трудоёмкости"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, factChart, "2. Оценка факта — Z-score по направлениям"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, compareChartPng, $"3. Сравнение {compareChart.VersionAName} / {compareChart.VersionBName}"));
            col.Item().PageBreak();
            col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, trendChart, "4. Тренды — факт vs прогноз"));
            if (includeAppendix)
            {
                col.Item().PageBreak();
                col.Item().Text("Приложение: прогноз по направлениям").SemiBold();
                col.Item().Element(c => QuestPdfReportComposer.ComposeHierarchicalTable(
                    c, "",
                    ["Направление", "База ч"],
                    planData.Forecast.Summary.Where(r => !r.IsTotal)
                        .Select(r => new HierarchicalPdfRow(r.Level, [r.DisplayName, r.BaseHours?.ToString("N0") ?? "—"]))));
            }
        },
        "Сводный отчёт",
        $"Проект ID: {projectId}",
        $"План v{planVersionId} · Факт v{factVersionId} · Сравнение {compareVersionAId}/{compareVersionBId}");
    }

    private byte[] BuildCompareSummaryPdf(
        VersionCompareResultDto compare,
        CompareChartResultDto chart,
        bool showOnlyDifferences,
        int top) =>
        QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeCompareSummary(col, compare, chart, showOnlyDifferences, top),
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

        var tableRows = summary
            .Where(r => r.FactPhysicalVolumeA.HasValue || r.FactPhysicalVolumeB.HasValue
                || r.PlannedLaborHoursA.HasValue || r.PlannedLaborHoursB.HasValue
                || r.FactLaborHoursA.HasValue || r.FactLaborHoursB.HasValue
                || r.FactZScoreA.HasValue || r.FactZScoreB.HasValue)
            .OrderByDescending(r => Math.Abs(r.FactZDelta ?? r.PlanZDelta ?? 0))
            .Select(r => new[]
            {
                r.Name,
                r.FactPhysicalVolumeA?.ToString("N2") ?? "—",
                r.FactPhysicalVolumeB?.ToString("N2") ?? "—",
                r.PlannedLaborHoursA?.ToString("N0") ?? "—",
                r.PlannedLaborHoursB?.ToString("N0") ?? "—",
                r.FactLaborHoursA?.ToString("N0") ?? "—",
                r.FactLaborHoursB?.ToString("N0") ?? "—",
                r.PlannedProductivityA?.ToString("N4") ?? "—",
                r.PlannedProductivityB?.ToString("N4") ?? "—",
                r.FactProductivityA?.ToString("N4") ?? "—",
                r.FactProductivityB?.ToString("N4") ?? "—",
                r.FactZScoreA?.ToString("N4") ?? "—",
                r.FactZScoreB?.ToString("N4") ?? "—",
            });

        col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
        col.Item().PaddingTop(8).Element(c => QuestPdfReportComposer.ComposeChartImage(c, chartPng, $"Изменение {chart.Metric} между версиями"));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Итого IIC",
            ["Направление", "ФО A", "ФО B", "План. труд A", "План. труд B", "Факт. труд A", "Факт. труд B",
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

        var tableRows = facility.Select(f => new[]
        {
            f.FacilityName,
            f.FactPhysicalVolumeA?.ToString("N2") ?? "—",
            f.FactPhysicalVolumeB?.ToString("N2") ?? "—",
            f.PlannedLaborHoursA?.ToString("N0") ?? "—",
            f.PlannedLaborHoursB?.ToString("N0") ?? "—",
            f.FactLaborHoursA?.ToString("N0") ?? "—",
            f.FactLaborHoursB?.ToString("N0") ?? "—",
            f.PlannedProductivityA?.ToString("N4") ?? "—",
            f.PlannedProductivityB?.ToString("N4") ?? "—",
            f.FactProductivityA?.ToString("N4") ?? "—",
            f.FactProductivityB?.ToString("N4") ?? "—",
            f.ZScoreA?.ToString("N4") ?? "—",
            f.ZScoreB?.ToString("N4") ?? "—",
        });

        col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chartPng, "Сравнение трудоёмкости по объектам"));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "По объектам",
            ["Объект", "ФО A", "ФО B", "План. труд A", "План. труд B", "Факт. труд A", "Факт. труд B",
                "План. произв. A", "План. произв. B", "Факт. произв. A", "Факт. произв. B", "Z A", "Z B"],
            tableRows));
    }

    private byte[] BuildCompareDetailPdf(
        VersionCompareResultDto compare,
        bool showOnlyDifferences,
        int top) =>
        QuestPdfReportComposer.BuildLandscapeReport(
            col => ComposeCompareDetail(col, compare, showOnlyDifferences, top),
            "Сравнение версий — Детально");

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

        var tableRows = detail.Select(d => new[]
        {
            d.FacilityName,
            d.PvCode,
            d.PvName,
            d.QuantityA?.ToString("N2") ?? "—",
            d.QuantityB?.ToString("N2") ?? "—",
            d.PlannedLaborHoursA?.ToString("N0") ?? "—",
            d.PlannedLaborHoursB?.ToString("N0") ?? "—",
            d.FactLaborHoursA?.ToString("N0") ?? "—",
            d.FactLaborHoursB?.ToString("N0") ?? "—",
            d.PlannedProductivityA?.ToString("N4") ?? "—",
            d.PlannedProductivityB?.ToString("N4") ?? "—",
            d.FactProductivityA?.ToString("N4") ?? "—",
            d.FactProductivityB?.ToString("N4") ?? "—",
            d.ZScoreA?.ToString("N4") ?? "—",
            d.ZScoreB?.ToString("N4") ?? "—",
            d.Presence,
        });

        col.Item().Element(c => QuestPdfReportComposer.ComposeChartImage(c, chartPng, "Наибольшие отклонения по позициям"));
        col.Item().PageBreak();
        col.Item().Element(c => QuestPdfReportComposer.ComposeTable(
            c, "Детально",
            ["Объект", "Код", "Наименование", "ФО A", "ФО B", "План. труд A", "План. труд B",
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

        var kpis = new List<ReportKpiItem>
        {
            new("План Z", FmtZ(report.Plan?.WeightedZScore)),
            new("Факт Z", FmtZ(report.Fact?.WeightedZScore)),
            new("Перцентиль (факт)", report.Fact?.WeightedZScorePercentile ?? "—"),
            new("Трудоёмкость факта", FmtHours(report.Fact?.TotalLaborHours)),
            new("Трудоёмкость плана", FmtHours(report.Plan?.TotalLaborHours)),
            new("Точность PD%", report.Accuracy.AveragePercent is null ? "—" : $"{report.Accuracy.AveragePercent:0.0}%"),
        };

        var topRows = (report.Fact ?? report.Plan)?.Directions
            .Where(d => d.ZScore.HasValue && (d.SubtreeHours ?? 0) > 0)
            .OrderByDescending(d => Math.Abs(d.ZScore!.Value) * (d.SubtreeHours ?? 0))
            .Take(12)
            .Select(d => (string[])
            [
                d.Name,
                d.ZScore?.ToString("N2") ?? "—",
                d.SubtreeHours?.ToString("N0") ?? "—",
                d.Interpretation ?? "—",
            ])
            .ToList() ?? [];

        var insightLines = report.Insights.Items
            .Take(8)
            .Select(i => $"{i.Title}: {i.Detail}")
            .ToList();

        return QuestPdfReportComposer.BuildLandscapeReport(col =>
        {
            col.Item().Element(c => QuestPdfReportComposer.ComposeKpiStrip(c, kpis));
            if (topRows.Count > 0)
            {
                col.Item().PaddingTop(10).Element(c => QuestPdfReportComposer.ComposeTable(
                    c,
                    "Ключевые направления (|Z| × часы)",
                    ["Направление", "Z", "Часы поддерева", "Интерпретация"],
                    topRows));
            }

            if (insightLines.Count > 0)
            {
                col.Item().PaddingTop(10).Text("Выводы и рекомендации").SemiBold().FontSize(11);
                foreach (var line in insightLines)
                    col.Item().PaddingTop(2).Text($"• {line}").FontSize(8);
            }
        },
        "Отчет по инженерной производительности",
        $"Проект: {report.Project.Code} — {report.Project.Name}",
        $"План версий: {report.Project.PlanVersionCount}; факт версий: {report.Project.FactVersionCount}",
        report.Plan is null ? "План: —" : $"План: {report.Plan.VersionName}",
        report.Fact is null ? "Факт: —" : $"Факт: {report.Fact.VersionName}");
    }
}
