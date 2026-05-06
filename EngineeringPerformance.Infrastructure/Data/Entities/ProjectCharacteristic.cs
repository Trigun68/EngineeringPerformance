using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class ProjectCharacteristic : DirectoryItem
{
    public ICollection<ProjectCharacteristicOption> Options { get; set; } = [];
}
