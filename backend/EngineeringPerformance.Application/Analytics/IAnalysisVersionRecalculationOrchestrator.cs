namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Оркестрация пересчёта трудоёмкости версий анализа после смены параметров/норм/справочников.
/// </summary>
public interface IAnalysisVersionRecalculationOrchestrator
{
    /// <summary>Пересчёт LI одной версии.</summary>
    Task<int> RecalculateVersionAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    /// <summary>Пересчёт LI одной версии с причиной для журнала.</summary>
    Task<int> RecalculateVersionAsync(
        int projectId,
        int versionId,
        string trigger,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Версии, чья дата (CreatedAt) попадает в период нормы: обновить снимок + LI.
    /// </summary>
    Task<int> AfterNormChangedAsync(
        int workDirectionId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        CancellationToken cancellationToken = default);

    /// <summary>Версии с привязкой к позиции каталога (КФО/БЕР/оборудование): пересчёт LI.</summary>
    Task<int> AfterCatalogAssessmentOrCoefficientChangedAsync(
        string resource,
        int catalogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Пересчёт сохранённых <c>LaborIntensity*</c> по всем версиям (актуальный движок).
    /// </summary>
    Task<int> RecalculateAllVersionsAsync(
        bool includeArchived = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VersionCalculationLogDto>> ListLogsAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}

public sealed record VersionCalculationLogDto(
    long Id,
    DateTime Utc,
    string Trigger,
    EngineeringPerformance.Domain.Norms.PlanningScenario PlanningScenario,
    int SnapshotRowCount,
    int PpvRowCount,
    int EquipmentRowCount,
    decimal BaseHoursSum,
    decimal OptimisticHoursSum,
    decimal PessimisticHoursSum);
