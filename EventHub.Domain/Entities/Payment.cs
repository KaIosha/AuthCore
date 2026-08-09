using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;

namespace EventHub.Domain.Entities
{
    public class Payment : ISoftDelete
    {
        [Key]
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Foreign Key & Navigation Property (1-to-1 optional)
        public Guid RegistrationId { get; set; }
        [ForeignKey("RegistrationId")]
        public Registration Registration { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
