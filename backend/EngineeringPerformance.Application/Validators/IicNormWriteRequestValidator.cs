using EngineeringPerformance.Application.Registers;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class IicNormWriteRequestValidator : AbstractValidator<IicNormWriteRequest>
{
    public IicNormWriteRequestValidator()
    {
        RuleFor(x => x.WorkDirectionId)
            .GreaterThan(0);

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty();

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);

        RuleFor(x => x.Performance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Performance.HasValue);

        RuleFor(x => x.Estimate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Estimate.HasValue);

        RuleFor(x => x.StandardDeviation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StandardDeviation.HasValue);

        RuleFor(x => x.UnitId)
            .GreaterThan(0)
            .When(x => x.UnitId.HasValue);

        RuleFor(x => x)
            .Must(x => x.Performance.HasValue || x.Estimate.HasValue || x.StandardDeviation.HasValue)
            .WithMessage("At least one norm value must be provided.");

        RuleForEach(x => x.History).SetValidator(new IicNormHistoryItemValidator());
    }

    private sealed class IicNormHistoryItemValidator : AbstractValidator<IicNormHistoryItem>
    {
        public IicNormHistoryItemValidator()
        {
            RuleFor(x => x.EffectiveFrom)
                .NotEmpty();

            RuleFor(x => x.EffectiveTo)
                .GreaterThan(x => x.EffectiveFrom)
                .When(x => x.EffectiveTo.HasValue);

            RuleFor(x => x.Performance)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Performance.HasValue);

            RuleFor(x => x.Estimate)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Estimate.HasValue);

            RuleFor(x => x.StandardDeviation)
                .GreaterThanOrEqualTo(0)
                .When(x => x.StandardDeviation.HasValue);

            RuleFor(x => x.UnitId)
                .GreaterThan(0)
                .When(x => x.UnitId.HasValue);
        }
    }
}
