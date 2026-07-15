namespace EngineeringPerformance.Domain.Norms;

/// <summary>Точка коридора ±1σ для визуализации N(V).</summary>
public sealed class NormBandPoint
{
    public decimal Volume { get; set; }

    public decimal Center { get; set; }

    public decimal Upper { get; set; }

    public decimal Lower { get; set; }
}
