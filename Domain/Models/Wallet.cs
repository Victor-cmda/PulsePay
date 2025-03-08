using Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Wallet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PendingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBalance { get; set; }

        [Required]
        public WalletType WalletType { get; set; }

        public bool IsDefault { get; set; }

        public DateTime LastUpdateAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<WalletTransaction> Transactions { get; set; }
    }
}