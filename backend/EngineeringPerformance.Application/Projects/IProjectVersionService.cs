using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public interface IProjectVersionService
{
    Task<IReadOnlyList<ProjectVersionDto>> ListAsync(int projectId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto?> GetByIdAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto> CreateAsync(int projectId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto?> UpdateAsync(int projectId, int versionId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

<<<<<<< HEAD
    /// <summary>
    /// Делает версию основной для её направления (Plan или Fact): снимает <c>IsMain</c> у всех
    /// остальных версий проекта с тем же <see cref="AnalysisDirection"/> и устанавливает у выбранной.
    /// Forecast-версии не поддерживаются (нет понятия «основная версия прогноза»).
    /// </summary>
    /// <exception cref="KeyNotFoundException">Версия не найдена в проекте.</exception>
    /// <exception cref="InvalidOperationException">Направление версии — Forecast.</exception>
    Task<ProjectVersionDto> SetMainAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
=======
    Task<ProjectVersionDto?> UnarchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default);

    Task<ProjectVersionDto?> SetPrimaryAsync(int projectId, int versionId, CancellationToken cancellationToken = default);
>>>>>>> origin/master

    Task<ProjectVersionDto> CopyAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        AnalysisDirection? targetDirection = null,
        CancellationToken cancellationToken = default);
}
