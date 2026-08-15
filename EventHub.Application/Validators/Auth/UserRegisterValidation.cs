using System;
using System.Collections.Generic;
using System.Text;
using EventHub.Application.Dtos.AuthDtos;
using FluentValidation;

namespace EventHub.Application.Validators.Auth
{
    public class UserRegisterValidation : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterValidation()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
             .NotEmpty().WithMessage("Password is required.")
             .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
             .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
             .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
             .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
             .Matches(@"[\^$*.\[\]{}()?""!@#%&/\\,><':;|_~`]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("ConfirmPassword is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

        }
    }
}
