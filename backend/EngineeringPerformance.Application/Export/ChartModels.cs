namespace EngineeringPerformance.Application.Export;

/// <summary>Подписи осей и отображение значений на графике.</summary>
public sealed record ChartRenderOptions(
    string? XAxisLabel = null,
    string? YAxisLabel = null,
    bool ShowValueLabels = true,
    bool ShowGrid = true,
    int? MaxLabelLength = null);

public sealed record ChartBarItem(string Label, double Value, string? Color = null);

public sealed record ChartGroupedBarItem(string Label, double ValueA, double ValueB, string? ColorA = null, string? ColorB = null);

public sealed record ChartLineSeries(string Name, IReadOnlyList<(DateTime X, double Y)> Points, string Color);

public sealed record ChartAreaSeries(string Name, IReadOnlyList<(DateTime X, double Y)> Points, string Color);

/// <summary><paramref name="SubValue"/> — вторая строка на титуле (стоимость ₽); на контентных KPI не рисуется.</summary>
public sealed record ReportKpiItem(string Label, string Value, string? SubValue = null);

/// <summary>Одна колонка сводки «план/факт» (вкладка «Отчёт») — паритет PlanFactSummary.tsx.</summary>
public sealed record PdfPlanFactColumn(
    string VersionLabel,
    string Hours,
    string ZScore,
    string Percentile,
    string Interpretation);

/// <summary>Данные для титульной страницы PDF — рендерится первой в каждом отчёте.</summary>
public sealed record PdfCoverInfo(
    PdfReportKind Kind,
    string ReportTitle,
    string Project,
    string VersionLabel,
    IReadOnlyList<ReportKpiItem> TeaserKpis,
    IReadOnlyList<string>? ExtraMeta = null);

/// <summary>Тип авто-вывода / callout-блока в PDF.</summary>
public enum PdfCalloutKind
{
    Risk,
    Attention,
    Strength,
    Data,
}

/// <summary>Краткий авто-вывод (риск/внимание/сильная сторона/данные) — максимум 3 на отчёт.</summary>
public sealed record PdfCallout(PdfCalloutKind Kind, string Title, string Detail);

public enum CompareReportSection
{
    Summary,
    Facility,
    Detail,
    Full,
}

public enum CombinedReportSection
{
    Forecast,
    Fact,
    Compare,
    Trends,
}
