using EngineeringPerformance.Application.Auth;

namespace EngineeringPerformance.Tests;

internal sealed class StubCurrentUserService : ICurrentUserService
{
    public StubCurrentUserService(int userId)
    {
        UserId = userId;
        IsAuthenticated = true;
    }

    public int? UserId { get; }

    public string? Username => "test-user";

    public int? RoleId => 1;

    public string? RoleName => "ProjectManager";

    public bool IsAuthenticated { get; }
}
