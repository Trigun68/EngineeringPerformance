namespace EngineeringPerformance.Application.Analytics;

public interface IFactAssessmentPipeline
{
    Task<AssessmentResultDto> EvaluateAsync(AssessmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пакетная оценка: данные версий загружаются батчами (без N+1),
    /// ошибка одной версии не срывает остальные.
    /// </summary>
    Task<BatchAssessmentResult> EvaluateManyAsync(
        IReadOnlyList<AssessmentRequest> requests,
        CancellationToken cancellationToken = default);

    IReadOnlyList<AssessmentFormulaDescriptor> ListFormulas();
}
