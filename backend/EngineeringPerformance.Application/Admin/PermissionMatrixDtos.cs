namespace EngineeringPerformance.Application.Admin;

public sealed record PermissionMatrixRoleDto(int Id, string Name);

public sealed record PermissionMatrixObjectDto(
    string ObjectName,
    string DisplayName,
    string? ParentGroup,
    int Level,
    IReadOnlyList<string> HierarchyPath,
    bool IsGroupHeader);

public sealed record PermissionMatrixDto(
    IReadOnlyList<string> Objects,
    IReadOnlyList<PermissionMatrixObjectDto> ObjectHierarchy,
    IReadOnlyList<PermissionMatrixRoleDto> Roles,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Matrix);

public sealed record PermissionMatrixUpdateRequest(
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Matrix);
