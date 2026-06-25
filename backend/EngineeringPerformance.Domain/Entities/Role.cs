using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class Role : DirectoryItem
{
    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}
