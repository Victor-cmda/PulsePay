using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class BankAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [StringLength(50)]
        public string BankName { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountType { get; set; } // Checking, Savings

        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; }

        [Required]
        [StringLength(10)]
        public string BranchNumber { get; set; }

        [StringLength(20)]
        public string? PIXKey { get; set; }

        [StringLength(20)]
        public string? PIXKeyType { get; set; } // CPF, CNPJ, EMAIL, PHONE, RANDOM

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}