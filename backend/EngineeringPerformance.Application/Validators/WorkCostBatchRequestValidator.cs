using FluentValidation;
using EngineeringPerformance.Application.Registers;

namespace EngineeringPerformance.Application.Validators;

public sealed class WorkCostBatchRequestValidator : AbstractValidator<WorkCostBatchRequest>
{
    public WorkCostBatchRequestValidator()
    {
        RuleFor(x => x.EffectiveFrom)
            .NotEqual(default(DateTime))
            .WithMessage("Дата «Действует с» обязательна.");

        RuleFor(x => x.Level)
            .IsInEnum()
            .WithMessage("Некорректный уровень каскада стоимости работ.");

        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("Список ставок обязателен.")
            .Must(items => items is { Count: > 0 })
            .WithMessage("Укажите хотя бы одну ставку.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.WorkDirectionId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Идентификатор направления не может быть отрицательным.");

            item.RuleFor(i => i.RatePerHour)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Ставка ₽/ч не может быть отрицательной.");
        });
    }
}
