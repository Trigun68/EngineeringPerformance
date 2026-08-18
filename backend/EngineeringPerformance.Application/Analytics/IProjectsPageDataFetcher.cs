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
    /// То же самое в сетевом виде: гриды «Детально» упакованы (ISS-019).
    /// Кэшируется отдельно от полного DTO — иначе на каждый запрос пришлось бы читать из кэша
    /// 50 МБ и паковать заново, что дороже сэкономленного трафика.
    /// </summary>
    Task<ProjectsPageDataEncodedDto> GetEncodedPageDataAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default);
}
