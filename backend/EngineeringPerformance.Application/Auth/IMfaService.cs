using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

public interface IMfaService
{
    bool IsAvailable { get; }

    string GenerateSecret();

    string GetQrCodeUri(User user, string secret);

    bool VerifyTotp(User user, string code);

    bool VerifyTotpSecret(string secret, string code);

    Task EnableMfaAsync(User user, string secret, CancellationToken cancellationToken = default);

    Task DisableMfaAsync(User user, CancellationToken cancellationToken = default);

    bool IsMfaRequired(User user);

    bool IsMfaEnabled(User user);

    Task ResetMfaAsync(User user, CancellationToken cancellationToken = default);
}
