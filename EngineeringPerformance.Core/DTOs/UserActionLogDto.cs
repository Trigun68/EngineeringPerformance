using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

public sealed record UserActionLogDto(
    int Id,
    int UserId,
    string UserName,
    DateTime Timestamp,
    UserActionType ActionType,
    string? EntityType,
    int? EntityId,
    string? Description,
    string? IpOrHost);
