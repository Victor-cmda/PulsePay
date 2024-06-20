namespace Application.DTOs.Pix
{
    public class PaymentPixResponseDto
    {
        public string PaymentId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string QrCode { get; set; }
        public string TransactionId { get; set; }
        public DateTime ExpirationQrCode { get; set; }
    }
}
