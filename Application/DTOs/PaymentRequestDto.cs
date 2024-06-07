namespace Application.DTOs
{
    public class PaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
    }
}
