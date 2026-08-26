using System.ComponentModel.DataAnnotations;

namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>Конфигурация анализа факта и норм (пороги без magic numbers в коде).</summary>
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public FactNormAnalysisOptions FactNormAnalysis { get; set; } = new();

    public AccuracyOptions Accuracy { get; set; } = new();

    public NormCreationOptions NormCreation { get; set; } = new();

    public void Validate()
    {
        FactNormAnalysis.Validate();
        Accuracy.Validate();
        NormCreation.Validate();
    }
}

public sealed class FactNormAnalysisOptions
{
    [Range(0.0, 1.0)]
    public double DeltaR2Threshold { get; set; } = 0.05;

    [Range(0.0, 1.0)]
    public double MinAcceptableR2 { get; set; } = 0.70;

    [Range(0.0, 10.0)]
    public double ParamDriftThreshold { get; set; } = 0.15;

    public double DeltaAicSignificant { get; set; } = 2.0;

    public double DeltaBicStrong { get; set; } = 6.0;

    [Range(0.0, 1.0)]
    public double ThresholdMeanPct { get; set; } = 0.10;

    [Range(0.0, 1.0)]
    public double ThresholdSigmaPct { get; set; } = 0.20;

    [Range(2, 10_000)]
    public int MinPointsForFit { get; set; } = 5;

    [Range(2, 10_000)]
    public int MinPointsForReliableFit { get; set; } = 10;

    [Range(0.0, 1.0)]
    public double PValueThreshold { get; set; } = 0.05;

    [Range(100, 10_000_000)]
    public int MaxPointsPerRequest { get; set; } = 100_000;

    public int MaxPeriodDays { get; set; } = 1825;

    public bool RemoveOutliers3Sigma { get; set; } = true;

    public void Validate()
    {
        if (MinPointsForFit < 2)
            throw new InvalidOperationException("Analytics:FactNormAnalysis:MinPointsForFit must be >= 2.");
        if (MinPointsForReliableFit < MinPointsForFit)
            throw new InvalidOperationException(
                "Analytics:FactNormAnalysis:MinPointsForReliableFit must be >= MinPointsForFit.");
        if (MaxPointsPerRequest < MinPointsForFit)
            throw new InvalidOperationException(
                "Analytics:FactNormAnalysis:MaxPointsPerRequest must be >= MinPointsForFit.");
        if (MaxPeriodDays < 1)
            throw new InvalidOperationException("Analytics:FactNormAnalysis:MaxPeriodDays must be >= 1.");
    }
}

public sealed class AccuracyOptions
{
    public int LowMaxDays { get; set; } = 180;

    public int MediumMaxDays { get; set; } = 365;

    public int LowMinPoints { get; set; } = 30;

    public int HighMinPoints { get; set; } = 50;

    public void Validate()
    {
        if (LowMaxDays < 1 || MediumMaxDays <= LowMaxDays)
            throw new InvalidOperationException(
                "Analytics:Accuracy:MediumMaxDays must be > LowMaxDays.");
        if (LowMinPoints < 1 || HighMinPoints < LowMinPoints)
            throw new InvalidOperationException(
                "Analytics:Accuracy:HighMinPoints must be >= LowMinPoints.");
    }
}

public sealed class NormCreationOptions
{
    public string DefaultStatus { get; set; } = "Draft";

    public bool AllowBackdatedEffectiveFrom { get; set; }

    public bool RequireConfirmOnSave { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultStatus))
            throw new InvalidOperationException("Analytics:NormCreation:DefaultStatus is required.");
    }
}
