namespace EngineeringPerformance.Application.Analytics;

public interface IFactAssessmentPipeline
{
    Task<AssessmentResultDto> EvaluateAsync(AssessmentRequest request, CancellationToken cancellationToken = default);

    IReadOnlyList<AssessmentFormulaDescriptor> ListFormulas();
}
