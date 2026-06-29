using EngineeringPerformance.Application.Projects;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class ProjectVersionWriteRequestValidator : AbstractValidator<ProjectVersionWriteRequest>
{
    public ProjectVersionWriteRequestValidator()
    {
        RuleFor(x => x.VersionName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Name)
            .MaximumLength(200);

        RuleFor(x => x.Direction)
            .IsInEnum();
    }
}
