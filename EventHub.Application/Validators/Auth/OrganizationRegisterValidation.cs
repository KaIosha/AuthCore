using EventHub.Application.Dtos.AuthDtos;
using FluentValidation;

namespace EventHub.Application.Validators.Auth
{
    public class OrganizationRegisterValidation : AbstractValidator<OrganizationRegisterDto>
    {
        public OrganizationRegisterValidation()
        {
            RuleFor(x => x.OrganizationOwnerFirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.OrganizationOwnerLastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.OrganizationOwnerUsername)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            RuleFor(x => x.OrganizationOwnerEmail)
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
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

            // Organization Details Validation
            RuleFor(x => x.OrganizationName)
                .NotEmpty().WithMessage("Organization name is required.")
                .MaximumLength(100).WithMessage("Organization name cannot exceed 100 characters.");
            
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");


            RuleFor(x => x.Logo)
                .Must(file => file == null || file.Length > 0).WithMessage("Logo cannot be empty.")
                .Must(file => file == null || file.ContentType == "image/jpeg" || file.ContentType == "image/png"|| file.ContentType == "image/jpg")
                    .WithMessage("Logo must be a JPEG or PNG image.");


            RuleFor(x => x.VerificationDocument)
                .NotNull().WithMessage("Verification document is required.")
                .Must(file => file.Length > 0).WithMessage("Verification document cannot be empty.")
                .Must(file => file.ContentType == "application/pdf").WithMessage("Verification document must be a PDF.");

        }
    }
}
