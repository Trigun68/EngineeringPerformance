using EngineeringPerformance.Application.Registers;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class IicNormWriteRequestValidator : AbstractValidator<IicNormWriteRequest>
{
    public IicNormWriteRequestValidator()
    {
        RuleFor(x => x.WorkDirectionId)
            .GreaterThan(0)
            .WithMessage("Не выбрано направление работ.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty()
            .WithMessage("Укажите дату «Действует с».");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Дата «Действует по» должна быть позже «Действует с».");

        RuleFor(x => x.Performance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Performance.HasValue)
            .WithMessage("Выработка не может быть отрицательной.");

        // Оценка (Estimate) в мигрированных данных EPMS может быть отрицательной.
        RuleFor(x => x.StandardDeviation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StandardDeviation.HasValue)
            .WithMessage("Отклонение не может быть отрицательным.");

        RuleFor(x => x.UnitId)
            .GreaterThan(0)
            .When(x => x.UnitId.HasValue)
            .WithMessage("Некорректная единица измерения.");

        RuleFor(x => x)
            .Must(HasAtLeastOneNormValue)
            .WithMessage("Укажите хотя бы одно значение нормы (выработку, оценку, σ или формулу).");

        RuleForEach(x => x.History).SetValidator(new IicNormHistoryItemValidator());
    }

    /// <summary>
    /// UI часто шлёт только parametric NormFormula без legacy Performance/Estimate/σ.
    /// </summary>
    private static bool HasAtLeastOneNormValue(IicNormWriteRequest x)
    {
        if (x.Performance.HasValue || x.Estimate.HasValue || x.StandardDeviation.HasValue)
            return true;

        if (x.NormFormula?.Coefficients is { Count: > 0 } coeffs)
        {
            foreach (var value in coeffs.Values)
            {
                if (value != 0)
                    return true;
            }

            // Линейная N=a: даже a=0 — явное значение формулы (пользователь сохранил форму).
            if (coeffs.Keys.Any(k => string.Equals(k, "a", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private sealed class IicNormHistoryItemValidator : AbstractValidator<IicNormHistoryItem>
    {
        public IicNormHistoryItemValidator()
        {
            RuleFor(x => x.EffectiveFrom)
                .NotEmpty()
                .WithMessage("Укажите дату «Действует с».");

            RuleFor(x => x.EffectiveTo)
                .GreaterThan(x => x.EffectiveFrom)
                .When(x => x.EffectiveTo.HasValue)
                .WithMessage("Дата «Действует по» должна быть позже «Действует с».");

            RuleFor(x => x.Performance)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Performance.HasValue);

            RuleFor(x => x.StandardDeviation)
                .GreaterThanOrEqualTo(0)
                .When(x => x.StandardDeviation.HasValue);

            RuleFor(x => x.UnitId)
                .GreaterThan(0)
                .When(x => x.UnitId.HasValue)
                .WithMessage("Некорректная единица измерения.");
        }
    }
}
