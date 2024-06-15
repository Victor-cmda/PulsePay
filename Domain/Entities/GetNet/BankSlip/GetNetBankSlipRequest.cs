namespace Domain.Entities.GetNet.BankSlip
{
    public class GetNetBankSlipRequest
    {
        public string SellerId { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public OrderDto Order { get; set; }
        public BankSlipDto Boleto { get; set; }
        public CustomerBankSlipDto Customer { get; set; }
    }
}
