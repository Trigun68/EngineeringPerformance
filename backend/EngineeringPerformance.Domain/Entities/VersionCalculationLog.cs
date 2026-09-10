using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Журнал persist-пересчёта версии (сценарий, Σ часов, без снимка ставок ₽).</summary>
public sealed class VersionCalculationLog
{
    public long Id { get; set; }

    public int ProjectAnalysisVersionId { get; set; }

    public DateTime Utc { get; set; } = DateTime.UtcNow;

    public string Trigger { get; set; } = "recalculate";

    public PlanningScenario PlanningScenario { get; set; }

    public int SnapshotRowCount { get; set; }

    public int PpvRowCount { get; set; }

    public int EquipmentRowCount { get; set; }

    public decimal BaseHoursSum { get; set; }

    public decimal OptimisticHoursSum { get; set; }

    public decimal PessimisticHoursSum { get; set; }

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;
}
