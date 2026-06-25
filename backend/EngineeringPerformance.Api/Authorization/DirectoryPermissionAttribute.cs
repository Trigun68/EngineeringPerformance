using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EngineeringPerformance.Api.Authorization;

/// <summary>
/// Проверка прав доступа для динамических directory-ресурсов.
/// Унифицирует подход с [HasPermission] для фиксированных ресурсов.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class DirectoryPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public AccessLevel Level { get; }

    public DirectoryPermissionAttribute(AccessLevel level)
    {
        Level = level;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var resource = context.RouteData.Values["resource"] as string;
        if (string.IsNullOrEmpty(resource))
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (!Application.Directories.DirectoryResourceCatalog.TryGet(resource, out var meta))
        {
            context.Result = new NotFoundResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<Application.Permissions.IPermissionService>();
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<Application.Auth.ICurrentUserService>();

        if (currentUser.UserId is not int userId)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasAccess = await permissionService.HasAccessAsync(userId, meta.Permission, Level, context.HttpContext.RequestAborted);
        if (!hasAccess)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
