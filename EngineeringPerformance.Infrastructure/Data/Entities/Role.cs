using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class Role : DirectoryItem
{
    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}
