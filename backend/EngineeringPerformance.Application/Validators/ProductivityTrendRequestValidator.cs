using EngineeringPerformance.Application.Analytics;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class ProductivityTrendRequestValidator : AbstractValidator<ProductivityTrendRequest>
{
    private static readonly HashSet<string> AllowedVolumeModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "median",
        "p10",
        "p90",
    };

    public ProductivityTrendRequestValidator()
    {
        RuleFor(x => x.VolumeMode)
            .Must(m => string.IsNullOrWhiteSpace(m) || AllowedVolumeModes.Contains(m))
            .WithMessage("VolumeMode must be median, p10 or p90.");

        RuleFor(x => x)
            .Must(x => !x.PeriodStart.HasValue || !x.PeriodEnd.HasValue || x.PeriodStart.Value < x.PeriodEnd.Value)
            .WithMessage("PeriodStart must be earlier than PeriodEnd.");

        RuleFor(x => x.PeriodEnd)
            .Must(end => !end.HasValue || end.Value.Date <= DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("PeriodEnd cannot be in the far future.");
    }
}
