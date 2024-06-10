namespace Application.DTOs
{
    public class PaymentPixRequestDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Document { get; set; }
        public string DocumentType { get; set; }
    }
}
