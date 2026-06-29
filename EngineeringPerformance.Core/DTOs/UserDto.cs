namespace EngineeringPerformance.Core.DTOs;

public sealed record UserDto(
    int Id,
    string Username,
    string DisplayName,
    int RoleId,
    string RoleName,
    string? WindowsUsername,
    bool IsActive);
