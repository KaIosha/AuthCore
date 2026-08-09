using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Organization : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public OrganizationStatus Status { get; set; }
        public string VerificationDocumentUrl { get; set; } = string.Empty;

        public Guid OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; } = null!;

        public ICollection<Event> Events { get; set; } = new List<Event>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
}
