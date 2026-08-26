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
        if (!TryResolveProjectId(context, out var projectId))
        {
            context.Result = new BadRequestObjectResult($"Parameter '{ProjectIdRouteKey}' is required.");
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

    private bool TryResolveProjectId(ActionExecutingContext context, out int projectId)
    {
        if (context.ActionArguments.TryGetValue(ProjectIdRouteKey, out var arg)
            && TryParseInt(arg, out projectId))
        {
            return true;
        }

        if (context.RouteData.Values.TryGetValue(ProjectIdRouteKey, out var routeRaw)
            && TryParseInt(routeRaw, out projectId))
        {
            return true;
        }

        var query = context.HttpContext.Request.Query[ProjectIdRouteKey].FirstOrDefault();
        if (int.TryParse(query, out projectId))
            return true;

        projectId = default;
        return false;
    }

    private static bool TryParseInt(object? value, out int projectId)
    {
        switch (value)
        {
            case int i:
                projectId = i;
                return true;
            case long l when l is >= int.MinValue and <= int.MaxValue:
                projectId = (int)l;
                return true;
            default:
                return int.TryParse(value?.ToString(), out projectId);
        }
    }
}
