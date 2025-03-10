using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class CustomerPayout
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public CustomerPayoutStatus Status { get; set; } = CustomerPayoutStatus.Pending;

        [Required]
        public DateTime RequestedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [Required]
        [StringLength(100)]
        public string PixKey { get; set; }

        [Required]
        [StringLength(20)]
        public string PixKeyType { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(500)]
        public string RejectionReason { get; set; }

        [Required]
        [StringLength(100)]
        public string ValidationId { get; set; }

        public DateTime? ValidatedAt { get; set; }

        [StringLength(50)]
        public string ConfirmedByAdminId { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        [StringLength(100)]
        public string PaymentId { get; set; }

        [StringLength(100)]
        public string PaymentProofId { get; set; }
    }
}
