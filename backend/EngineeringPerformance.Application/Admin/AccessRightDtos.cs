using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Admin;

public sealed record AccessRightDto(
    int Id,
    int RoleId,
    string RoleName,
    string ObjectName,
    AccessLevel AccessLevel);

public sealed record UpdateAccessRightRequest(AccessLevel AccessLevel);
