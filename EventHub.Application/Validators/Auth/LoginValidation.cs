using EventHub.Application.Dtos.AuthDtos;
using FluentValidation;

namespace EventHub.Application.Validators.Auth
{
    public class LoginValidation : AbstractValidator<LoginDto>
    {
        public LoginValidation() 
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
