using Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Deposit
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DepositStatus Status { get; set; } = DepositStatus.Pending;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }

        [Column(TypeName = "text")]
        public string QrCode { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "PIX";

        [Column(TypeName = "text")]
        public string Notes { get; set; }

        [StringLength(100)]
        public string ExternalReference { get; set; }

        [StringLength(50)]
        public string PaymentProvider { get; set; }

        [StringLength(100)]
        public string ReceiptId { get; set; }
    }
}
