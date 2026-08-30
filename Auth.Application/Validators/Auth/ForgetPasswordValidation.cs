using System;
using System.Collections.Generic;
using System.Text;
using Auth.Application.Dtos.AuthDtos;
using FluentValidation;

namespace Auth.Application.Validators.Auth
{
    public class ForgetPasswordValidation : AbstractValidator<ForgetPasswordDto>
    {
        public ForgetPasswordValidation()
        {
            RuleFor(x=>x.Email)
               .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}
