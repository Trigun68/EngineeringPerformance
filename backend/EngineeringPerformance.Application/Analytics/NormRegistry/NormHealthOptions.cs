using System.ComponentModel.DataAnnotations;

namespace EngineeringPerformance.Application.Analytics.NormRegistry;

/// <summary>Конфигурация health-check нормативной базы (Analytics:NormHealth).</summary>
public sealed class NormHealthOptions
{
    public const string SectionName = "Analytics:NormHealth";

    public bool Enabled { get; set; } = true;

    public int CacheTtlMinutes { get; set; } = 5;

    public NormHealthRuleOptions OverlapConflict { get; set; } = new() { Enabled = true, Severity = "Error" };

    public NormHealthRuleOptions CoverageGap { get; set; } = new() { Enabled = true, Severity = "Warning" };

    public NormHealthRuleOptions NoActiveNorm { get; set; } = new() { Enabled = true, Severity = "Error" };

    public NormHealthRuleOptions StaleActive { get; set; } = new() { Enabled = true, Severity = "Warning" };

    public NormHealthRuleOptions DuplicateNorm { get; set; } = new()
    {
        Enabled = true,
        Severity = "Warning",
        DupParamTolerance = 0.05,
    };

    public NormHealthRuleOptions ZeroSigma { get; set; } = new()
    {
        Enabled = true,
        Severity = "Error",
        MinValidSigma = 0.000001,
    };

    public NormHealthRuleOptions InvalidParams { get; set; } = new() { Enabled = true, Severity = "Error" };

    public NormHealthRuleOptions UnusedNorm { get; set; } = new()
    {
        Enabled = true,
        Severity = "Info",
        UnusedWindowDays = 180,
    };

    public NormHealthRuleOptions StaleFuture { get; set; } = new()
    {
        Enabled = true,
        Severity = "Info",
        DraftMaxAgeDays = 90,
    };

    public NormHealthRuleOptions BrokenChain { get; set; } = new()
    {
        Enabled = true,
        Severity = "Warning",
        ChainToleranceDays = 1,
    };
}

public sealed class NormHealthRuleOptions
{
    public bool Enabled { get; set; } = true;

    public string Severity { get; set; } = "Warning";

    [Range(0.0, 1.0)]
    public double DupParamTolerance { get; set; } = 0.05;

    [Range(0.0, 1.0)]
    public double MinValidSigma { get; set; } = 0.000001;

    [Range(1, 3650)]
    public int UnusedWindowDays { get; set; } = 180;

    [Range(1, 3650)]
    public int DraftMaxAgeDays { get; set; } = 90;

    [Range(0, 30)]
    public int ChainToleranceDays { get; set; } = 1;
}
