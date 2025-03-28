using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Refund
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [ForeignKey("TransactionId")]
        public virtual Transaction Transaction { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; }

        [StringLength(50)]
        public string ExternalReference { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [StringLength(255)]
        public string FailReason { get; set; }

        [StringLength(255)]
        public string TransactionReceipt { get; set; }

        public Guid RefundWalletId { get; set; }
    }
}