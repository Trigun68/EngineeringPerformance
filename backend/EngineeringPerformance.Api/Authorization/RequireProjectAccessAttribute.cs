using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EngineeringPerformance.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireProjectAccessAttribute : Attribute, IAsyncActionFilter
{
    public RequireProjectAccessAttribute(AccessLevel level = AccessLevel.Read)
    {
        Level = level;
    }

    public AccessLevel Level { get; }

    public string ProjectIdRouteKey { get; set; } = "projectId";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue(ProjectIdRouteKey, out var raw)
            || !int.TryParse(raw?.ToString(), out var projectId))
        {
            context.Result = new BadRequestObjectResult($"Route parameter '{ProjectIdRouteKey}' is required.");
            return;
        }

        var visibility = context.HttpContext.RequestServices.GetRequiredService<Application.Projects.IProjectVisibilityService>();
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<Application.Auth.ICurrentUserService>();

        if (currentUser.UserId is not int userId
            || !await visibility.CanAccessProjectAsync(userId, projectId, Level))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
