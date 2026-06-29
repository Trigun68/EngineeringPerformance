using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IAuthenticationService
{
    Task<UserDto?> TryAutoLoginAsync();

    string? GetCurrentWindowsUsername();

    Task<UserDto?> LoginAsync(string username, string password);
}
