using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user);

    string GenerateMfaSessionToken(User user);

    int? ValidateMfaSessionToken(string token);

    DateTime GetExpiration();
}
