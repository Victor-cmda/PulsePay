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
    }
}
