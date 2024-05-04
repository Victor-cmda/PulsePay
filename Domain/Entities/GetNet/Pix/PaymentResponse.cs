namespace Domain.Entities.GetNet.Pix
{
    public class PaymentResponse
    {
        public string payment_id { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public AdditionalData additional_data { get; set; }
    }

    public class AdditionalData
    {
        public string transaction_id { get; set; }
        public string qr_code { get; set; }
        public DateTime creation_date_qrcode { get; set; }
        public DateTime expiration_date_qrcode { get; set; }
        public string psp_code { get; set; }
    }
}
