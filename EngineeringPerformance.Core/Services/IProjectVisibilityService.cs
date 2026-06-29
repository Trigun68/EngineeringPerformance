using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.Services;

public interface IProjectVisibilityService
{
    Task<bool> CanUserAccessProjectAsync(UserDto user, int projectId);

    Task<IReadOnlyList<ProjectDto>> GetAccessibleProjectsAsync(UserDto user);

    Task<IReadOnlyList<int>> GetProjectUserIdsAsync(int projectId);

    Task AddUserToProjectAsync(int projectId, int userId);

    Task RemoveUserFromProjectAsync(int projectId, int userId);

    /// <summary>
    /// Создаёт или обновляет разрешение пользователя на проект с указанным уровнем доступа.
    /// Идемпотентен: повторный вызов с тем же уровнем — no-op.
    /// </summary>
    Task AddOrUpdateUserPermissionAsync(int projectId, int userId, AccessLevel level);

    /// <summary>
    /// Возвращает пользователей с разрешениями на проект вместе с уровнем доступа и ролью.
    /// Используется UI для отображения матрицы участников проекта.
    /// </summary>
    Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId);
}
