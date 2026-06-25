using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public sealed record ProjectPermissionDto(
    int UserId,
    string Username,
    string DisplayName,
    string RoleName,
    AccessLevel Level);

public sealed record ProjectPermissionUpsertRequest(int UserId, AccessLevel Level);

public sealed record ProjectPermissionsBulkRequest(IReadOnlyList<ProjectPermissionUpsertRequest> Participants);
