namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>Z-параметры сценариев планирования для группы физ. объёмов IIC в рамках версии проекта.</summary>
public sealed class ProjectVersionPlanningParameter
{
    public int Id { get; set; }

    public int ProjectAnalysisVersionId { get; set; }

    public int IicWorkDirectionId { get; set; }

    public decimal ZPessimistic { get; set; } = 0.5m;

    public decimal ZBase { get; set; }

    public decimal ZOptimistic { get; set; } = -0.5m;

    public decimal? ZActual { get; set; }

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public IicWorkDirection IicWorkDirection { get; set; } = null!;
}
