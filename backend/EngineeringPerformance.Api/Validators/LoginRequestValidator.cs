using EngineeringPerformance.Api.Controllers;
using FluentValidation;

namespace EngineeringPerformance.Api.Validators;

public sealed class LoginRequestValidator : AbstractValidator<AuthController.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(512);
    }
}
