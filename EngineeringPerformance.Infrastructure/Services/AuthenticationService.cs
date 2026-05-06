using System.Runtime.Versioning;
using System.Security.Principal;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Exceptions;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IDbContextFactory<AppDbContext> contextFactory, ILogger<AuthenticationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    [SupportedOSPlatform("windows")]
    public async Task<UserDto?> TryAutoLoginAsync()
    {
        try
        {
            var windowsUsername = WindowsIdentity.GetCurrent().Name;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.WindowsUsername == windowsUsername && u.IsActive);

            if (user is null)
            {
                _logger.LogInformation("Auto-login failed: Windows user not found in directory");
                return null;
            }

            _logger.LogInformation("Auto-login successful for userId={UserId}", user.Id);
            return MapToDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-login failed due to database error");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    public string? GetCurrentWindowsUsername()
    {
        try
        {
            return WindowsIdentity.GetCurrent().Name;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserDto?> LoginAsync(string username, string password)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user is null)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        if (user.PasswordHash is null)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            throw new AuthenticationException("Invalid credentials");
        }

        _logger.LogInformation("Manual login successful for userId={UserId}", user.Id);
        return MapToDto(user);
    }

    public async Task<bool> VerifyConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Username,
        user.DisplayName,
        user.RoleId,
        user.Role.Name,
        user.WindowsUsername,
        user.IsActive);
}
