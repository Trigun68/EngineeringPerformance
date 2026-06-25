using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Projects;

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectDto>> GetAllForUserAsync(int userId, IReadOnlyList<int> accessibleProjectIds, bool isAdministrator, CancellationToken cancellationToken = default);

    Task<ProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Project?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    void Update(Project project);
}
