using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class EventSession : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }

        //Navigation Property
        public Guid EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
}
