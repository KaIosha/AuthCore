using Microsoft.AspNetCore.Http;

namespace EventHub.Application.Dtos.AuthDtos
{
    public class OrganizationRegisterDto
    {
        // Organization Owner Details
        public string OrganizationOwnerFirstName { get; set; } = string.Empty;
        public string OrganizationOwnerLastName { get; set; } = string.Empty;
        public string OrganizationOwnerUsername { get; set; } = string.Empty;
        public string OrganizationOwnerEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public IFormFile? ProfilePhoto { get; set; }



        // Organization Details
        public string OrganizationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile? Logo { get; set; }
        public IFormFile VerificationDocument { get; set; } = null!;


    }

}
