using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class AccessRight
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string ObjectName { get; set; } = string.Empty;

    public AccessLevel AccessLevel { get; set; }

    public Role Role { get; set; } = null!;
}
