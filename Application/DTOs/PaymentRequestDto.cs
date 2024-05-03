namespace Application.DTOs
{
    public class PaymentRequestDto
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
    }
}
