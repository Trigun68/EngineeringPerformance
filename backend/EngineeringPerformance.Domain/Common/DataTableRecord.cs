namespace EngineeringPerformance.Domain.Common;

public abstract class DataTableRecord
{
    public int Id { get; set; }

    public int ProjectAnalysisVersionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
