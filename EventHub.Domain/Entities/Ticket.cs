using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Ticket : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        // Foreign Key & Navigation Property (1-to-1)
        public Guid RegistrationId { get; set; }
        [ForeignKey("RegistrationId")]
        public Registration Registration { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
