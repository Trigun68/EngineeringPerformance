namespace EngineeringPerformance.Domain.Entities;

public sealed class UserActionLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpOrHost { get; set; }
    public bool IsArchived { get; set; }
    public User User { get; set; } = null!;
}
