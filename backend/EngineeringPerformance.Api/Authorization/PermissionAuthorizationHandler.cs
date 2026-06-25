using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EngineeringPerformance.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(PermissionType permission, AccessLevel level)
    {
        Permission = permission;
        Level = level;
    }

    public PermissionType Permission { get; }

    public AccessLevel Level { get; }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;

    public PermissionAuthorizationHandler(ICurrentUserService currentUser, IPermissionService permissionService)
    {
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (_currentUser.UserId is not int userId)
            return;

        if (await _permissionService.HasAccessAsync(userId, requirement.Permission, requirement.Level))
            context.Succeed(requirement);
    }
}

public static class PermissionPolicyExtensions
{
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (PermissionType permission in Enum.GetValues<PermissionType>())
        {
            foreach (AccessLevel level in Enum.GetValues<AccessLevel>())
            {
                if (level == AccessLevel.None)
                    continue;

                options.AddPolicy(
                    $"Permission:{permission}:{level}",
                    policy => policy.Requirements.Add(new PermissionRequirement(permission, level)));
            }
        }
    }
}
