namespace EngineeringPerformance.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<(UserDto? User, string? Error)> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<(UserDto? User, string? Error)> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> UnarchiveAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> ChangeOwnPasswordAsync(
        int userId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> ChangePasswordForUserAsync(
        int targetUserId,
        string newPassword,
        CancellationToken cancellationToken = default);
}
