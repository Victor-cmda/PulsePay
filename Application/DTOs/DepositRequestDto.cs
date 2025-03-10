using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class DepositRequestDto
    {
        [Required]
        public Guid SellerId { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required]
        public string SellerName { get; set; }

        [Required]
        [EmailAddress]
        public string SellerEmail { get; set; }

        [Required]
        public string SellerDocument { get; set; }

        [Required]
        public string SellerDocumentType { get; set; }
    }

    public class DepositDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string TransactionId { get; set; }
        public string QrCode { get; set; }
        public string PaymentMethod { get; set; }
    }
}
