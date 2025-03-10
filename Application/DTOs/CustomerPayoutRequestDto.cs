using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CustomerPayoutRequestDto
    {
        public Guid SellerId { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; }

        [Required]
        public string CustomerDocument { get; set; }

        [Required]
        public string CustomerDocumentType { get; set; }

        [Required]
        public string PixKey { get; set; }

        [Required]
        public string PixKeyType { get; set; } // CPF, CNPJ, EMAIL, PHONE, RANDOM

        public string Description { get; set; }
    }

    public class CustomerPayoutDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerDocument { get; set; }
        public string CustomerDocumentType { get; set; }
        public string PixKey { get; set; }
        public string PixKeyType { get; set; }
        public string Description { get; set; }
        public string RejectionReason { get; set; }
        public string PixInfoValidated { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string ValidatedBy { get; set; }
        public string ConfirmedBy { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string PaymentProofId { get; set; }
    }

    public class PixKeyValidationDto
    {
        public bool IsValid { get; set; }
        public string PixKey { get; set; }
        public string PixKeyType { get; set; }
        public string KeyOwnerName { get; set; }
        public string KeyOwnerDocument { get; set; }
        public string BankName { get; set; }
        public string ValidationId { get; set; }
        public string ErrorMessage { get; set; }
        public string Notes { get; set; }
        public bool ManuallyValidated { get; set; }
        public string ValidatedBy { get; set; }
    }
}
