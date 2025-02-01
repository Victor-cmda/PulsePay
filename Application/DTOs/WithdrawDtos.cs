using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class WithdrawCreateDto
    {
        [Required]
        public Guid SellerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string WithdrawMethod { get; set; }

        [Required]
        public Guid BankAccountId { get; set; }
    }

    public class WithdrawUpdateDto
    {
        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public string? TransactionReceipt { get; set; }
    }

    public class WithdrawResponseDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string WithdrawMethod { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public BankAccount BankAccount { get; set; }
        public string? FailureReason { get; set; }
        public string? TransactionReceipt { get; set; }
    }

    public class WithdrawSummaryDto
    {
        public decimal TotalWithdrawn { get; set; }
        public int TotalRequests { get; set; }
        public decimal PendingAmount { get; set; }
        public DateTime Period { get; set; }
    }

}
