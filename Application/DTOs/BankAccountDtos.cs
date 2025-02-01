using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class BankAccountCreateDto
    {
        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [StringLength(100)]
        public string BankName { get; set; }

        [Required]
        [StringLength(3)]
        public string BankCode { get; set; }

        [Required]
        public BankAccountType AccountType { get; set; }

        // Campos específicos para TED
        [StringLength(20)]
        public string? AccountNumber { get; set; }

        [StringLength(10)]
        public string? BranchNumber { get; set; }

        [Required]
        [StringLength(14)]
        public string DocumentNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string AccountHolderName { get; set; }

        // Campos específicos para PIX
        [StringLength(100)]
        public string? PixKey { get; set; }

        public PixKeyType? PixKeyType { get; set; }
    }

    public class BankAccountUpdateDto
    {
        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(3)]
        public string? BankCode { get; set; }

        // Campos específicos para TED
        [StringLength(20)]
        public string? AccountNumber { get; set; }

        [StringLength(10)]
        public string? BranchNumber { get; set; }

        [StringLength(100)]
        public string? AccountHolderName { get; set; }

        // Campos específicos para PIX
        [StringLength(100)]
        public string? PixKey { get; set; }

        public PixKeyType? PixKeyType { get; set; }
    }

    public class BankAccountResponseDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public BankAccountType AccountType { get; set; }

        // Campos específicos para TED
        public string? AccountNumber { get; set; }
        public string? BranchNumber { get; set; }

        // Campos específicos para PIX
        public string? PixKey { get; set; }
        public PixKeyType? PixKeyType { get; set; }

        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class BankAccountValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}
