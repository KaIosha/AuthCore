using Auth.Application.Dtos.AuthDtos;
using FluentValidation;

namespace Auth.Application.Validators.Auth
{
    public class ResetPasswordValidation : AbstractValidator<ResetPasswordDto> 
    {
        public ResetPasswordValidation()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Enter the Reset code");

            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.NewPassword)
             .NotEmpty().WithMessage("Password is required.")
             .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
             .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
             .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
             .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
             .Matches(@"[\^$*.\[\]{}()?""!@#%&/\\,><':;|_~`]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirmation Password is required.")
                .Equal(x => x.NewPassword).WithMessage("The new password and confirmation password do not match");
        }
    }
}
