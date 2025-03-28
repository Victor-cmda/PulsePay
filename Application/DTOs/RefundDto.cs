using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RefundRequestDto
    {
        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string Reason { get; set; }

        [StringLength(50)]
        public string ExternalReference { get; set; }
    }

    public class RefundResponseDto
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string ExternalReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string FailReason { get; set; }
    }
}
