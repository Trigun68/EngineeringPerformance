namespace EngineeringPerformance.Application.Export;

public interface IVisualReportService
{
    Task<byte[]> ExportForecastSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportForecastFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactSummaryPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportFactFacilityPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAnalysisPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    /// <summary>PDF вкладки «Отчёт»: сводка план/факт по основным версиям, шкала Z, топ-5 направлений, диаграммы и таблица физобъёмов.</summary>
    Task<byte[]> ExportProjectReportPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<byte[]> ExportComparePdfAsync(
        int projectId,
        int versionAId,
        int versionBId,
        CompareReportSection section = CompareReportSection.Full,
        bool showOnlyDifferences = false,
        int top = 20,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportTrendsPdfAsync(
        int projectId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string formulaId = "v1-docset-proportional",
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCombinedPdfAsync(
        int projectId,
        int planVersionId,
        int factVersionId,
        int compareVersionAId,
        int compareVersionBId,
        string formulaId = "v1-docset-proportional",
        bool includeAppendix = false,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportPerformanceReportPdfAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Сводный отчёт «план/факт» по основным версиям проекта (вкладка «Отчёт»).</summary>
    Task<byte[]> ExportProjectReportPdfAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
}
