namespace EngineeringPerformance.Domain.Entities;

public sealed class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public int? ReplacedByTokenId { get; set; }

    public string? RevocationReason { get; set; }

    public User User { get; set; } = null!;

    public RefreshToken? ReplacedByToken { get; set; }
}
