using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Registration : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public int Quantity { get; set; }
        public RegistrationStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }

        
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public Guid EventSessionId { get; set; }
        [ForeignKey("EventSessionId")]  
        public EventSession EventSession { get; set; } = null!;

        // 1-to-1 Optional Navigations
        public Ticket? Ticket { get; set; }
        public Payment? Payment { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
