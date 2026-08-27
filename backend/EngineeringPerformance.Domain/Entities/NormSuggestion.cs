namespace EngineeringPerformance.Domain.Entities;

public enum NormSuggestionType
{
    ChangeFormulaType = 1,
    AdjustParameters = 2,
    NormIsActual = 3,
}

public enum NormSuggestionStatus
{
    Pending = 1,
    Implemented = 2,
    Rejected = 3,
}

public enum NormSuggestionConfidence
{
    Low = 1,
    Medium = 2,
    High = 3,
}

public enum NormChangeAction
{
    Created = 1,
    EffectiveToChanged = 2,
    Archived = 3,
}

/// <summary>Предложение по изменению нормы ИИС по результатам анализа факта.</summary>
public sealed class NormSuggestion
{
    public int Id { get; set; }

    public int NormId { get; set; }

    public NormSuggestionType SuggestionType { get; set; }

    public NormSuggestionStatus Status { get; set; } = NormSuggestionStatus.Pending;

    public string ProposedFormulaType { get; set; } = string.Empty;

    public string ProposedParametersJson { get; set; } = "{}";

    public double ProposedStdDeviation { get; set; }

    public double ProposedMeanPerformance { get; set; }

    public double CurrentR2 { get; set; }

    public double ProposedR2 { get; set; }

    public double DeltaR2 { get; set; }

    public NormSuggestionConfidence Confidence { get; set; }

    public int DataPointsCount { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public string? ProjectFilterJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IicWorkDirectionNorm Norm { get; set; } = null!;
}

/// <summary>Append-only аудит изменений норм.</summary>
public sealed class NormChangeAudit
{
    public long Id { get; set; }

    public int NormId { get; set; }

    public NormChangeAction Action { get; set; }

    public string ChangedBy { get; set; } = string.Empty;

    public string? OldValueJson { get; set; }

    public string? NewValueJson { get; set; }

    public int? SourceSuggestionId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public IicWorkDirectionNorm Norm { get; set; } = null!;
}
