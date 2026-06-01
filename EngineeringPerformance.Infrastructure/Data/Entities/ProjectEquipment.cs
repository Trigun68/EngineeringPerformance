using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>Данные оборудования по проекту и версии анализа (загрузка из Excel).</summary>
public sealed class ProjectEquipment : DataTableRecord
{
    public int ProjectId { get; set; }

    public int EquipmentId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? QuantityInWork { get; set; }

    public string? PositionNumber { get; set; }

    public string? FacilityCode { get; set; }

    public string? FacilityName { get; set; }

    public string? DocumentSet { get; set; }

    public string? DiagramNumber { get; set; }

    public string? ShortName { get; set; }

    public string? CatalogName { get; set; }

    public string? TypeMark { get; set; }

    public string? CatalogId { get; set; }

    public string? Tag { get; set; }

    public string? FullEquipmentId { get; set; }

    public string? SupplyStandard { get; set; }

    public string? OlReference { get; set; }

    public string? EstimationBasis { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public Equipment Equipment { get; set; } = null!;
}
