namespace EngineeringPerformance.Application.Analytics;

public interface IProjectsPageDataFetcher
{
    /// <summary>
    /// Загружает данные для 9 гридов одним вызовом.
    /// </summary>
    /// <param name="compareVersionId">Версия B для гридов сравнения (7–9); если null — Compare пустой.</param>
    Task<ProjectsPageDataDto> GetProjectsPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);
}
