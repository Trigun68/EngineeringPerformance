using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EngineeringPerformance.Infrastructure.Audit;

public sealed class UserActionLogger : IUserActionLogger
{
    private readonly IUserActionLogService _logService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserActionLogger(
        IUserActionLogService logService,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _logService = logService;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogAsync(
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int userId)
            return Task.CompletedTask;

        return LogForUserAsync(userId, actionType, entityType, entityId, description, cancellationToken: cancellationToken);
    }

    public Task LogForUserAsync(
        int userId,
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        string? ipOrHost = null,
        CancellationToken cancellationToken = default) =>
        _logService.LogAsync(
            userId,
            actionType,
            entityType,
            entityId,
            description,
            ipOrHost ?? ResolveClientHost(),
            cancellationToken);

    private string? ResolveClientHost()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Environment.MachineName;

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return Truncate(forwarded.Split(',')[0].Trim(), 64);

        return Truncate(context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Host.Value, 64);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
}
