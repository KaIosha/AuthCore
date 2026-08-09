using EventHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>, ISoftDelete
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }

        // Soft Delete Attributes
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Organization? Organization { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
