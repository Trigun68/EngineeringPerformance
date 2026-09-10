namespace EngineeringPerformance.Application.Auth;

public sealed record SpnegoResolvedUser(
    int UserId,
    string Username,
    string DisplayName,
    string RoleName,
    int RoleId);

/// <summary>Резолв Windows/SPNEGO-имени в активного пользователя без DbContext в Api.</summary>
public interface ISpnegoIdentityResolver
{
    Task<SpnegoResolvedUser?> ResolveAsync(string remoteUser, CancellationToken cancellationToken = default);
}
