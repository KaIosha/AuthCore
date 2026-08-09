using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Event : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime RegistrationDeadline { get; set; } = DateTime.Now;
        public Decimal Price { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Pending;

        // Navigation Properties
        // Foreign Keys & Navigation Properties
        public Guid OrganizationId { get; set; }
        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; } = null!;

        public Guid CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<EventSession> EventSessions { get; set; } = new List<EventSession>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
