using EngineeringPerformance.Application.Analytics;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class ZTrendRequestValidator : AbstractValidator<ZTrendRequest>
{
    private static readonly HashSet<string> AllowedWeightModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "basisHours",
    };

    public ZTrendRequestValidator()
    {
        RuleFor(x => x.WeightMode)
            .Must(m => string.IsNullOrWhiteSpace(m) || AllowedWeightModes.Contains(m))
            .WithMessage("WeightMode must be basisHours.");

        RuleFor(x => x)
            .Must(x => !x.PeriodStart.HasValue || !x.PeriodEnd.HasValue || x.PeriodStart.Value < x.PeriodEnd.Value)
            .WithMessage("PeriodStart must be earlier than PeriodEnd.");

        RuleFor(x => x.PeriodEnd)
            .Must(end => !end.HasValue || end.Value.Date <= DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("PeriodEnd cannot be in the far future.");
    }
}
