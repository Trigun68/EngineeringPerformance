namespace EngineeringPerformance.Application.Auth;

public interface ICurrentUserService
{
    int? UserId { get; }

    string? Username { get; }

    int? RoleId { get; }

    string? RoleName { get; }

    bool IsAuthenticated { get; }
}
