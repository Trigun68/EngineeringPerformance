using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

/// <summary>
/// Валидатор для CreateProjectRequest — соответствие ограничениям БД.
/// </summary>
public sealed class CreateProjectRequestValidator : AbstractValidator<Projects.DTOs.CreateProjectRequest>
{
    private const int MaxCodeLength = 100;
    private const int MaxNameLength = 500;

    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код проекта обязателен.")
            .MaximumLength(MaxCodeLength).WithMessage($"Код проекта не может превышать {MaxCodeLength} символов.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование проекта обязательно.")
            .MaximumLength(MaxNameLength).WithMessage($"Наименование не может превышать {MaxNameLength} символов.");

        // Опциональные поля — только ограничение длины
        RuleFor(x => x.WorkNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.WorkNumber));
        RuleFor(x => x.ContractNumber).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ContractNumber));
        RuleFor(x => x.CalendarPlanNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.CalendarPlanNumber));
        RuleFor(x => x.Type).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Type));
        RuleFor(x => x.Customer).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Customer));
        RuleFor(x => x.Contractor).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Contractor));
        RuleFor(x => x.Gip).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Gip));
        RuleFor(x => x.WorkStatus).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.WorkStatus));
    }
}
