namespace EngineeringPerformance.Application.Analytics;

public interface IPerformanceService
{
    Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Производительность версии с выбором источника часов LI (PlannedHours vs ActualHours).
    /// </summary>
    Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(
        int projectId,
        int versionId,
        LaborHoursKind hoursKind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сбрасывает процессный кеш иерархии направлений ИИС, из которого строится производительность.
    /// Должен вызываться при любом изменении справочника ИИС (в т.ч. после Invalidate/Upsert/Remove кеша направлений).
    /// </summary>
    void InvalidateWorkDirectionHierarchyCache();
}
