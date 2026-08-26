namespace EngineeringPerformance.Application.Analytics;

public interface IPerformanceService
{
    /// <param name="useActualHours">
    /// Если <c>true</c> — строки трудоёмкости берутся из <c>ActualHours</c> (факт) вместо
    /// <c>PlannedHours</c> (план); нормы/снимок версии не меняются. Используется для параллельного
    /// расчёта «Z-факт» по той же версии (см. <see cref="IProjectReportSummaryService"/>).
    /// </param>
    Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(int projectId, int versionId, bool useActualHours = false);

    /// <summary>
    /// Сбрасывает процессный кеш иерархии направлений ИИС, из которого строится производительность.
    /// Должен вызываться при любом изменении справочника ИИС (в т.ч. после Invalidate/Upsert/Remove кеша направлений).
    /// </summary>
    void InvalidateWorkDirectionHierarchyCache();
}
