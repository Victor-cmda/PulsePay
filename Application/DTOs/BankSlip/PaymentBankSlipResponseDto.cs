namespace Application.DTOs.BankSlip
{
    public class PaymentBankSlipResponseDto
    {
        public string OrderId { get; set; }
        public string TypefulLine { get; set; }
        public string BarCode { get; set; }
        public string HrefPdf { get; set; }
    }
}
