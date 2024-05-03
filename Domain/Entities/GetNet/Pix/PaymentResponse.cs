namespace Domain.Entities.GetNet.Pix
{
    public class PaymentResponse
    {
        public string PaymentId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public AdditionalData AdditionalData { get; set; }
    }

    public class AdditionalData
    {
        public string TransactionId { get; set; }
        public string QrCode { get; set; }
        public DateTime CreationDateQrCode { get; set; }
        public DateTime ExpirationDateQrCode { get; set; }
        public string PspCode { get; set; }
    }
}
