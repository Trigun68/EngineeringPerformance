using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Analytics;

public interface IProjectsPageRawDataLoader
{
    Task<ProjectsPageVersionBundle> LoadAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Батч-загрузка bundle для нескольких версий (портфельная аналитика):
    /// константное число SQL-запросов вместо N+1 по версиям.
    /// Недоступные пользователю проекты пропускаются молча.
    /// Ключ результата — VersionId.
    /// </summary>
    Task<IReadOnlyDictionary<int, ProjectsPageVersionBundle>> LoadManyAsync(
        IReadOnlyList<(int ProjectId, int VersionId)> keys,
        CancellationToken cancellationToken = default);
}
