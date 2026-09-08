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
}
