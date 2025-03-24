using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class WithdrawRequestDto
    {
        [Required]
        public Guid SellerId { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        [Required]
        public Guid BankAccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required]
        public string Method { get; set; }
    }

    public class WithdrawDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string WithdrawMethod { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid BankAccountId { get; set; }
        public BankAccountDto BankAccount { get; set; }
        public string RejectionReason { get; set; }
        public string TransactionReceipt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class BankAccountDto
    {
        public Guid Id { get; set; }
        public string BankName { get; set; }
        public string AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string BranchNumber { get; set; }
        public string PixKey { get; set; }
        public string PixKeyType { get; set; }
        public string AccountHolderName { get; set; }
    }
}
