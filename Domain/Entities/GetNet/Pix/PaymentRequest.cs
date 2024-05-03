namespace Domain.Entities.GetNet.Pix
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
    }
}
