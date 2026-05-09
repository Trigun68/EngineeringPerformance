using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

// Фильтр для выборки записей журнала действий.
// Любое поле = null означает «без ограничения».
public sealed record UserActionLogFilter(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? UserId = null,
    UserActionType? ActionType = null,
    string? EntityType = null,
    int? EntityId = null);
