namespace Application.DTOs.CreditCard
{
    public class PaymentCreditCardResponseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string OrderId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public Credit Credit { get; set; }
    }

    public class Credit
    {
        public int AuthorizationCode { get; set; }
        public DateTime AuthorizedAt { get; set; }
        public int TransactionId { get; set; }
        public string? Message { get; set; }
    }
}
