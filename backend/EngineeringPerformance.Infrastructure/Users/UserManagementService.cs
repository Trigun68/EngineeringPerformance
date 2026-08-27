using System.Security.Cryptography;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Users;

public sealed class UserManagementService(
    AppDbContext context,
    IAccountLockoutService accountLockoutService,
    IMfaService mfaService) : IUserManagementService
{
    private const string TemporaryPasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
    private readonly AppDbContext _context = context;
    private readonly IAccountLockoutService _accountLockoutService = accountLockoutService;
    private readonly IMfaService _mfaService = mfaService;

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();
        if (!includeArchived)
            query = query.Where(u => !u.IsArchived);

        return await query
            .OrderBy(u => u.Username)
            .Select(u => Map(u))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Id == id)
            .Select(u => Map(u))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles
            .AsNoTracking()
            .Where(r => !r.IsArchived)
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .ToListAsync(cancellationToken);

    public async Task<(UserDto? User, string? Error)> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request.Username, request.DisplayName, request.RoleId);
        if (validationError is not null)
            return (null, validationError);

        if (await UsernameExistsAsync(request.Username.Trim(), null, cancellationToken))
            return (null, "Пользователь с таким логином уже существует");

        if (!await RoleExistsAsync(request.RoleId, cancellationToken))
            return (null, "Роль не найдена");

        var user = new User
        {
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Name = request.DisplayName.Trim(),
            RoleId = request.RoleId,
            WindowsUsername = string.IsNullOrWhiteSpace(request.WindowsUsername) ? null : request.WindowsUsername.Trim(),
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            CreatedAt = DateTime.UtcNow,
        };

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken), null);
    }

    public async Task<(UserDto? User, string? Error)> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request.Username, request.DisplayName, request.RoleId);
        if (validationError is not null)
            return (null, validationError);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return (null, "Пользователь не найден");

        if (await UsernameExistsAsync(request.Username.Trim(), id, cancellationToken))
            return (null, "Пользователь с таким логином уже существует");

        if (!await RoleExistsAsync(request.RoleId, cancellationToken))
            return (null, "Роль не найдена");

        user.Username = request.Username.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.Name = request.DisplayName.Trim();
        user.RoleId = request.RoleId;
        user.WindowsUsername = string.IsNullOrWhiteSpace(request.WindowsUsername) ? null : request.WindowsUsername.Trim();
        user.IsActive = request.IsActive;
        user.ModifiedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(id, cancellationToken), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return (false, "Пользователь не найден");

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить пользователя: есть связанные записи в системе");
        }
    }

    public async Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return false;

        user.IsArchived = true;
        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return false;

        user.IsArchived = false;
        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Success, string? Error)> ChangeOwnPasswordAsync(
        int userId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateNewPassword(newPassword);
        if (validationError is not null)
            return (false, validationError);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "Пользователь не найден");

        if (string.IsNullOrEmpty(user.PasswordHash))
            return (false, "Смена пароля недоступна для данной учётной записи");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return (false, "Неверный текущий пароль");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordForUserAsync(
        int targetUserId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateNewPassword(newPassword);
        if (validationError is not null)
            return (false, validationError);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null)
            return (false, "Пользователь не найден");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> UnlockAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
            return false;

        await _accountLockoutService.UnlockAsync(user, cancellationToken);
        return true;
    }

    public async Task<MfaResetStatus> ResetMfaAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!_mfaService.IsAvailable)
            return MfaResetStatus.Disabled;

        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
            return MfaResetStatus.UserNotFound;

        await _mfaService.ResetMfaAsync(user, cancellationToken);
        return MfaResetStatus.Success;
    }

    public async Task<(string? TemporaryPassword, string? Error)> ResetPasswordAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
            return (null, "Пользователь не найден");

        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        user.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return (temporaryPassword, null);
    }

    private static string? ValidateNewPassword(string newPassword) =>
        string.IsNullOrWhiteSpace(newPassword) ? "Новый пароль обязателен" : null;

    private static string GenerateTemporaryPassword()
    {
        Span<char> password = stackalloc char[16];
        for (var index = 0; index < password.Length; index++)
        {
            password[index] = TemporaryPasswordAlphabet[
                RandomNumberGenerator.GetInt32(TemporaryPasswordAlphabet.Length)];
        }

        return new string(password);
    }

    private static string? ValidateRequest(string username, string displayName, int roleId)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName))
            return "Логин и имя обязательны";
        if (roleId <= 0)
            return "Роль обязательна";
        return null;
    }

    private async Task<bool> UsernameExistsAsync(string username, int? excludeId, CancellationToken cancellationToken) =>
        await _context.Users.AnyAsync(
            u => u.Username.ToLower() == username.ToLower() && (excludeId == null || u.Id != excludeId),
            cancellationToken);

    private async Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken) =>
        await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsArchived, cancellationToken);

    private static UserDto Map(User u) =>
        new(
            u.Id,
            u.Username,
            u.DisplayName,
            u.RoleId,
            u.Role.Name,
            u.WindowsUsername,
            u.IsActive,
            u.IsArchived);
}
