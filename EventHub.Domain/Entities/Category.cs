using System.ComponentModel.DataAnnotations;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Category : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Event> Events { get; set; } = new List<Event>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
