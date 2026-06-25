namespace EngineeringPerformance.Domain.Entities;

public sealed class UserSetting
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
