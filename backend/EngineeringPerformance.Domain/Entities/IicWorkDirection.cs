using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class IicWorkDirection : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? UnitId { get; set; }

    public IicWorkDirection? Parent { get; set; }

    public ICollection<IicWorkDirection> Children { get; set; } = [];

    public UnitOfMeasurement? Unit { get; set; }
}
