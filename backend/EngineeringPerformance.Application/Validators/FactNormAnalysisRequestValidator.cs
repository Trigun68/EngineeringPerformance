using EngineeringPerformance.Application.Analytics.FactNorm;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class FactNormAnalysisRequestValidator : AbstractValidator<FactNormAnalysisRequest>
{
    public FactNormAnalysisRequestValidator()
    {
        RuleFor(x => x.PeriodEnd)
            .GreaterThan(x => x.PeriodStart)
            .WithMessage("PeriodStart must be before PeriodEnd.");

        RuleFor(x => x.PeriodEnd)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("PeriodEnd cannot be in the future.");
    }
}

public sealed class CreateNormFromSuggestionRequestValidator : AbstractValidator<CreateNormFromSuggestionRequest>
{
    public CreateNormFromSuggestionRequestValidator()
    {
        RuleFor(x => x.SuggestionId).GreaterThan(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.StdDeviation)
            .GreaterThan(0)
            .When(x => x.StdDeviation.HasValue);
    }
}
