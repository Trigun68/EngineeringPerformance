using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Domain.Entities;

public sealed class AuthAuditLog
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int? UserId { get; set; }

    public string? Identifier { get; set; }

    public AuthEventType EventType { get; set; }

    public bool Success { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public string? Details { get; set; }

    public User? User { get; set; }
}
