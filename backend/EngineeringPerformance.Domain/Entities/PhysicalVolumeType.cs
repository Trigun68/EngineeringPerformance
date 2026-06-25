using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class PhysicalVolumeType : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
