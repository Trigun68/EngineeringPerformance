using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user);

    DateTime GetExpiration();
}
