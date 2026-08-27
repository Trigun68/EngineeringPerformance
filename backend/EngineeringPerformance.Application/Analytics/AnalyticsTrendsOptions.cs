namespace EngineeringPerformance.Application.Analytics;

/// <summary>Конфигурация трендового анализа Z (Analytics:Trends).</summary>
public sealed class AnalyticsTrendsOptions
{
    public const string SectionName = "Analytics:Trends";

    public string WeightMode { get; set; } = "basisHours";
    public int GapThresholdDays { get; set; } = 90;
    public double LightenStep { get; set; } = 0.18;
    public double ZClipMax { get; set; } = 5.0;
    public double MinSigma { get; set; } = 1e-6;
    public bool AggregateSameDay { get; set; } = true;
    public int CacheTtlMinutes { get; set; } = 5;
}
