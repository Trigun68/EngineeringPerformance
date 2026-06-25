using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Audit;

public sealed record UserActionLogDto(
    int Id,
    int UserId,
    string UserName,
    DateTime Timestamp,
    UserActionType ActionType,
    string? EntityType,
    int? EntityId,
    string? Description,
    string? IpOrHost,
    bool IsArchived = false);

/// <summary>Фильтр журнала: null-поля означают «без ограничения».</summary>
public sealed record UserActionLogFilter(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? UserId = null,
    UserActionType? ActionType = null,
    string? EntityType = null,
    int? EntityId = null);
