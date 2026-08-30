using Auth.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Auth.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>, ISoftDelete
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }

        // Email confirmation code (6 digits) + when it expires
        public string? EmailConfirmationCode { get; set; }
        public DateTime? EmailConfirmationCodeExpiresAt { get; set; }
        public int EmailConfirmationCodeAttempts { get; set; } = 0;

        // Soft Delete Attributes
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiresAt { get; set; }
        public int PasswordResetCodeAttempts { get; set; } = 0;

        // Navigation property
        public Organization? Organization { get; set; }
    }
}
