using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>
/// V2-C: единая модель доступа к проекту. Заменяет связку
/// <c>Project.Visibility</c> + <c>ProjectUserAccess</c>.
/// Семантика: «нет записи = нет доступа», кроме пользователей с ролью
/// <see cref="EngineeringPerformance.Core.Enums.UserRole.Administrator"/> —
/// они видят все проекты независимо от <see cref="ProjectPermission"/>.
/// </summary>
public sealed class ProjectPermission
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public AccessLevel Level { get; set; } = AccessLevel.Read;

    public Project Project { get; set; } = null!;

    public User User { get; set; } = null!;
}
