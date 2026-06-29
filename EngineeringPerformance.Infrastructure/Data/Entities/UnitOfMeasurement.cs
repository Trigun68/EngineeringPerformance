using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class UnitOfMeasurement : DirectoryItem
{
    public string Description { get; set; } = string.Empty;

    public ICollection<IicWorkDirection> WorkDirections { get; set; } = [];

    public ICollection<IicWorkDirectionNorm> Norms { get; set; } = [];
}
