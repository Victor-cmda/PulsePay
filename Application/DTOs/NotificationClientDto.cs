namespace Application.DTOs
{
    public class NotificationClientDto
    {
        public Guid Id { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public string FailReason { get; set; }
        public string Type { get; set; } = "payment";
        public string RefundId { get; set; }
        public string RefundReason { get; set; }
        public string Description { get; set; }
    }
}