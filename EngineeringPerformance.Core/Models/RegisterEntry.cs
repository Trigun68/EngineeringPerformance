namespace EngineeringPerformance.Core.Models;

public abstract class RegisterEntry
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
