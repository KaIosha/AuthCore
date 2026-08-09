using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Favorite : ISoftDelete
    {
        public DateTime CreatedAt { get; set; }

        // Composite Foreign Keys & Navigation Properties
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public Guid EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
