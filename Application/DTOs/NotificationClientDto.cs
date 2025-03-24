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
    }
}
