using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RefundRequestDto
    {
        [Required]
        public string Transaction_id { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string Reason { get; set; }
    }

    public class RefundResponseDto
    {
        public Guid refund_id { get; set; }
        public Guid Transaction_id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
