using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

public sealed record AccessRightDto(
    int Id,
    int RoleId,
    string RoleName,
    string ObjectName,
    AccessLevel AccessLevel);
