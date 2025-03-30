namespace Application.DTOs.Pix
{
    public class PaymentPixResponseDto
    {
        public Guid PaymentId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string QrCode { get; set; }
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public DateTime ExpirationQrCode { get; set; }
    }
}
