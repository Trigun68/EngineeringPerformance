using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OtpNet;
using System.Security.Cryptography;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class MfaService(
    AppDbContext context,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<MfaOptions> options,
    IUserActionLogger actionLogger) : IMfaService
{
    private readonly MfaOptions _options = options.Value;
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector(options.Value.SecretEncryptionPurpose);

    public bool IsAvailable => _options.Enabled;

    public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string GetQrCodeUri(User user, string secret)
    {
        var issuer = Uri.EscapeDataString(_options.Issuer);
        var accountName = Uri.EscapeDataString(user.Username);
        return $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}&period={_options.TotpWindowSeconds}";
    }

    public bool VerifyTotp(User user, string code)
    {
        if (string.IsNullOrWhiteSpace(user.MfaSecret))
            return false;

        try
        {
            return VerifyTotpSecret(_protector.Unprotect(user.MfaSecret), code);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public bool VerifyTotpSecret(string secret, string code)
    {
        if (!IsValidCode(code) || string.IsNullOrWhiteSpace(secret))
            return false;

        try
        {
            var totp = new Totp(
                Base32Encoding.ToBytes(secret),
                step: _options.TotpWindowSeconds);
            return totp.VerifyTotp(
                code,
                out _,
                new VerificationWindow(_options.TotpTolerance, _options.TotpTolerance));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public async Task EnableMfaAsync(User user, string secret, CancellationToken cancellationToken = default)
    {
        user.MfaSecret = _protector.Protect(secret);
        user.MfaEnabled = true;
        await context.SaveChangesAsync(cancellationToken);
        await actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.MfaEnable,
            cancellationToken: cancellationToken);
    }

    public async Task DisableMfaAsync(User user, CancellationToken cancellationToken = default)
    {
        user.MfaSecret = null;
        user.MfaEnabled = false;
        await context.SaveChangesAsync(cancellationToken);
        await actionLogger.LogForUserAsync(
            user.Id,
            UserActionType.MfaDisable,
            cancellationToken: cancellationToken);
    }

    public bool IsMfaRequired(User user) =>
        _options.Enabled
        && user.Role is not null
        && _options.RequiredRoles.Contains(user.Role.Name, StringComparer.OrdinalIgnoreCase);

    public bool IsMfaEnabled(User user) => _options.Enabled && user.MfaEnabled;

    public async Task ResetMfaAsync(User user, CancellationToken cancellationToken = default) =>
        await DisableMfaAsync(user, cancellationToken);

    public async Task<User?> GetActiveUserByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

    private static bool IsValidCode(string code) =>
        code.Length == 6 && code.All(char.IsAsciiDigit);
}
