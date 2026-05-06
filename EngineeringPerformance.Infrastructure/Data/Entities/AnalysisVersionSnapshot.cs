namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>
/// V2-B: снимок норм, действовавших на момент создания версии анализа проекта.
/// При расчёте Z-score исторические отчёты должны опираться на нормы,
/// которые были актуальны на <see cref="ProjectAnalysisVersion.CreatedAt"/>,
/// а не на текущее состояние справочника <see cref="IicWorkDirectionNorm"/>.
/// Без снимка любая правка справочника норм «плыла» бы по всем версиям.
/// </summary>
public sealed class AnalysisVersionSnapshot
{
    public int Id { get; set; }

    public int ProjectAnalysisVersionId { get; set; }

    public int WorkDirectionId { get; set; }

    public int NormId { get; set; }

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public IicWorkDirection WorkDirection { get; set; } = null!;

    public IicWorkDirectionNorm Norm { get; set; } = null!;
}
