using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EngineeringPerformance.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(PermissionType permission, AccessLevel level = AccessLevel.Read)
    {
        Policy = $"Permission:{permission}:{level}";
    }
}
