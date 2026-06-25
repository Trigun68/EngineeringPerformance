using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

/// <summary>
/// Валидатор для DirectoryWriteRequest — соответствие ограничениям БД и бизнес-правилам.
/// </summary>
public sealed class DirectoryWriteRequestValidator : AbstractValidator<Directories.DirectoryWriteRequest>
{
    public DirectoryWriteRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование обязательно для заполнения.")
            .MaximumLength(500).WithMessage("Наименование не может превышать 500 символов.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Порядок сортировки не может быть отрицательным.");

        RuleFor(x => x.ParentId)
            .GreaterThan(0).When(x => x.ParentId.HasValue)
            .WithMessage("ParentId должен быть положительным числом.");
    }
}
