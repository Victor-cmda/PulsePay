namespace Application.DTOs
{
    public class TransactionDataDto
    {
        public Guid Id { get; set; }
        public string Customer { get; set; }
        public Guid SellerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; }
        public double Amount { get; set; }
        public string PaymentType { get; set; }
        public string Description { get; set; }
    }
}