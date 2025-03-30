namespace Application.DTOs
{
    public class NotificationDto
    {
        public string TransactionId { get; set; }
        public string PaymentId { get; set; }
        public string PaymentType { get; set; }
        public string CustomerId { get; set; }
        public string Status { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionTimestamp { get; set; }
        public string OrderId { get; set; }

        // Additional fields for refunds
        public string RefundId { get; set; }
        public string RefundReason { get; set; }

        // Additional fields for failures
        public string FailReason { get; set; }

        // Additional timestamps
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        // Amount information
        public decimal? Amount { get; set; }
    }
}