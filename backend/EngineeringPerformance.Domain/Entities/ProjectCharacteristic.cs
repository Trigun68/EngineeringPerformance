using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class ProjectCharacteristic : DirectoryItem
{
    public ICollection<ProjectCharacteristicOption> Options { get; set; } = [];
}
