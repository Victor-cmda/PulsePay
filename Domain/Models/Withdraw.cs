using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Withdraw
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public WithdrawStatus Status { get; set; } = WithdrawStatus.Pending;

        [Required]
        [StringLength(50)]
        public string WithdrawMethod { get; set; } // PIX, TED, etc.

        [Required]
        public DateTime RequestedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [Required]
        public Guid BankAccountId { get; set; }

        [ForeignKey("BankAccountId")]
        public virtual BankAccount BankAccount { get; set; }

        [StringLength(500)]
        public string RejectionReason { get; set; }

        [Column(TypeName = "text")]
        public string TransactionReceipt { get; set; }

        [StringLength(50)]
        public string ApprovedBy { get; set; } // ID do admin que aprovou

        public DateTime? ApprovedAt { get; set; }

        public Guid WalletId { get; set; }

        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }
    }
}
