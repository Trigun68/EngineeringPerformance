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

    /// <summary>
    /// Сетевой вид без строк «Детально» (пустые encoded-гриды с колонками).
    /// Полный упакованный ответ кэшируется отдельно от DTO экспорта.
    /// </summary>
    Task<ProjectsPageDataEncodedDto> GetEncodedPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Только упакованные гриды «Детально» из того же кэша, что и <see cref="GetEncodedPageDataAsync"/>.</summary>
    Task<ProjectsPageDetailsEncodedDto> GetEncodedPageDetailsAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);
}
