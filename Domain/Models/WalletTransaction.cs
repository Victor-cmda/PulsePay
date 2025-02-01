using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class WalletTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; } // Credit, Debit, Withdraw, Refund

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // Completed, Pending, Failed

        public Guid? ReferenceId { get; set; } // ID da Transaction ou Withdraw relacionada

        [StringLength(50)]
        public string? ReferenceType { get; set; } // Transaction, Withdraw

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewBalance { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}