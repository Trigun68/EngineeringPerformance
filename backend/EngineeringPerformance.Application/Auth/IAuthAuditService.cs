using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Auth;

public interface IAuthAuditService
{
    Task WriteAsync(
        AuthEventType eventType,
        bool success,
        int? userId = null,
        string? identifier = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}
