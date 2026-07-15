using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class UpdateProjectRequestValidator : AbstractValidator<Projects.DTOs.UpdateProjectRequest>
{
    private const int MaxCodeLength = 50;
    private const int MaxNameLength = 500;

    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код проекта обязателен.")
            .MaximumLength(MaxCodeLength).WithMessage($"Код проекта не может превышать {MaxCodeLength} символов.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование проекта обязательно.")
            .MaximumLength(MaxNameLength).WithMessage($"Наименование не может превышать {MaxNameLength} символов.");

        RuleFor(x => x.WorkNumber).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.WorkNumber));
        RuleFor(x => x.ContractNumber).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.ContractNumber));
        RuleFor(x => x.CalendarPlanNumber).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.CalendarPlanNumber));
        RuleFor(x => x.Type).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Type));
        RuleFor(x => x.Customer).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Customer));
        RuleFor(x => x.Contractor).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Contractor));
        RuleFor(x => x.Gip).MaximumLength(300).When(x => !string.IsNullOrEmpty(x.Gip));
        RuleFor(x => x.WorkStatus).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.WorkStatus));
        RuleFor(x => x.Program).MaximumLength(300).When(x => !string.IsNullOrEmpty(x.Program));
    }
}
