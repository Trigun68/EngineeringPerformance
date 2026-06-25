using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Audit;

/// <summary>
/// Запись в журнал от имени текущего пользователя (best-effort).
/// </summary>
public interface IUserActionLogger
{
    Task LogAsync(
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task LogForUserAsync(
        int userId,
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        string? ipOrHost = null,
        CancellationToken cancellationToken = default);
}
