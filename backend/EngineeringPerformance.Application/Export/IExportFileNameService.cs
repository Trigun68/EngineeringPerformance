namespace EngineeringPerformance.Application.Export;

/// <summary>Собирает каноническое имя файла экспорта по метаданным проекта и версии.</summary>
public interface IExportFileNameService
{
    Task<string> ForVersionAsync(
        int projectId,
        int versionId,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default);

    Task<string> ForVersionsAsync(
        int projectId,
        IReadOnlyList<int> versionIds,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default);

    Task<string> ForProjectAsync(
        int projectId,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default);

    Task<string> ForPerformanceReportAsync(
        int projectId,
        int? planVersionId,
        int? factVersionId,
        CancellationToken cancellationToken = default);
}
