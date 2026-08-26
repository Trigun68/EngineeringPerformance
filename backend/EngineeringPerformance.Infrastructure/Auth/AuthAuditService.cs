using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class AuthAuditService(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthAuditOptions> options) : IAuthAuditService
{
    private readonly AppDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly AuthAuditOptions _options = options.Value;

    public async Task WriteAsync(
        AuthEventType eventType,
        bool success,
        int? userId = null,
        string? identifier = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var request = _httpContextAccessor.HttpContext?.Request;
        _context.AuthAuditLogs.Add(new AuthAuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Identifier = Trim(identifier, 256),
            EventType = eventType,
            Success = success,
            IpAddress = Trim(_httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(), 64),
            UserAgent = Trim(request?.Headers.UserAgent.ToString(), 1024),
            CorrelationId = Trim(request?.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? _httpContextAccessor.HttpContext?.TraceIdentifier, 128),
            Details = Trim(details, 4000)
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
