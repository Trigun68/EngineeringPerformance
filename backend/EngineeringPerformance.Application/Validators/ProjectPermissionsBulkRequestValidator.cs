using EngineeringPerformance.Application.Projects;
using FluentValidation;

namespace EngineeringPerformance.Application.Validators;

public sealed class ProjectPermissionsBulkRequestValidator : AbstractValidator<ProjectPermissionsBulkRequest>
{
    public ProjectPermissionsBulkRequestValidator()
    {
        RuleFor(x => x.Participants)
            .NotNull()
            .Must(x => x.Count <= 500)
            .WithMessage("Too many participants in a single request.");

        RuleForEach(x => x.Participants)
            .SetValidator(new ProjectPermissionUpsertRequestValidator());

        RuleFor(x => x.Participants)
            .Must(HaveUniqueUsers)
            .WithMessage("Participants must contain unique userId values.");
    }

    private static bool HaveUniqueUsers(IReadOnlyList<ProjectPermissionUpsertRequest> participants)
    {
        if (participants.Count <= 1)
            return true;

        return participants
            .GroupBy(x => x.UserId)
            .All(g => g.Count() == 1);
    }

    private sealed class ProjectPermissionUpsertRequestValidator : AbstractValidator<ProjectPermissionUpsertRequest>
    {
        public ProjectPermissionUpsertRequestValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            RuleFor(x => x.Level)
                .IsInEnum();
        }
    }
}
