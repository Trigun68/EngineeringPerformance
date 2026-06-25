namespace EngineeringPerformance.Application.Users;

public sealed record CurrentUserProfileDto(
    int Id,
    string Login,
    string DisplayName,
    string? WindowsAccount,
    string Role,
    bool IsActive);

public sealed record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword);

public sealed record AdminChangePasswordRequest(string NewPassword);
