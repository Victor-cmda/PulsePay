using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [ForeignKey("TransactionId")]
        public virtual Transaction Transaction { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int SendAttempts { get; set; } = 0;

        [StringLength(20)]
        public string SendStatus { get; set; } = "PENDING";

        public DateTime NextAttempt { get; set; }

        public DateTime? LastAttempt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? ClientUrl { get; set; }
    }
}
