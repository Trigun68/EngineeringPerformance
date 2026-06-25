using System.Security.Claims;
using EngineeringPerformance.Application.Auth;
using Microsoft.AspNetCore.Http;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Username => User?.FindFirstValue(ClaimTypes.Name);

    public int? RoleId => int.TryParse(User?.FindFirstValue("role_id"), out var id) ? id : null;

    public string? RoleName => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
