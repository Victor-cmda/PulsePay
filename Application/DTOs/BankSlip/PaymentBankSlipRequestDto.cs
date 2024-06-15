namespace Application.DTOs.BankSlip
{
    public class PaymentBankSlipRequestDto
    {
        public string SellerId { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
    }
}
