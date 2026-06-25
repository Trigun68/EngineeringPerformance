namespace EngineeringPerformance.Application.Users;

public sealed record UserDto(
    int Id,
    string Username,
    string DisplayName,
    int RoleId,
    string RoleName,
    string? WindowsUsername,
    bool IsActive,
    bool IsArchived);

public sealed record RoleDto(int Id, string Name, string? Description);

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    int RoleId,
    string? Password,
    string? WindowsUsername,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    string Username,
    string DisplayName,
    int RoleId,
    string? Password,
    string? WindowsUsername,
    bool IsActive);
