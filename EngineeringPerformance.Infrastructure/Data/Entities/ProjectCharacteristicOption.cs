namespace EngineeringPerformance.Infrastructure.Data;

public sealed class ProjectCharacteristicOption
{
    public int Id { get; set; }

    public int CharacteristicId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Среднее значение PD в процентах (например 7.20 = 7.2%).</summary>
    public decimal? ValuePercent { get; set; }

    public int SortOrder { get; set; }

    public ProjectCharacteristic? Characteristic { get; set; }
}
